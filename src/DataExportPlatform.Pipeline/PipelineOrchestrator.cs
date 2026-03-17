using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.Data;
using DataExportPlatform.Infrastructure.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataExportPlatform.Pipeline;

public class PipelineOrchestrator : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly DataContextCache _cache;
    private readonly ILogger<PipelineOrchestrator> _logger;

    // Manual trigger signal
    private TaskCompletionSource<bool> _manualTrigger = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public PipelineOrchestrator(
        IServiceProvider services,
        DataContextCache cache,
        ILogger<PipelineOrchestrator> logger)
    {
        _services = services;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Triggers a manual pipeline run from a controller or external caller.</summary>
    public Task TriggerManualRunAsync(CancellationToken ct)
    {
        _manualTrigger.TrySetResult(true);
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PipelineOrchestrator started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRun();
            _logger.LogInformation("Next scheduled run in {Delay}.", delay);

            // Wait for either the scheduled time or a manual trigger
            var delayTask = Task.Delay(delay, stoppingToken);
            var triggerTask = _manualTrigger.Task;

            var winner = await Task.WhenAny(delayTask, triggerTask);

            if (stoppingToken.IsCancellationRequested)
                break;

            // Reset trigger for next cycle
            _manualTrigger = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            bool isManual = winner == triggerTask;
            _logger.LogInformation("Pipeline run starting ({Trigger}).", isManual ? "manual" : "scheduled");

            try
            {
                await RunPipelineAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unhandled exception in pipeline run.");
            }
        }

        _logger.LogInformation("PipelineOrchestrator stopped.");
    }

    private async Task RunPipelineAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exportJobs = scope.ServiceProvider.GetRequiredService<IEnumerable<IExportJob>>().ToList();

        // ── Step 1: Fetch all data sources in parallel ──────────────────────────
        _logger.LogInformation("Step 1: Fetching data sources.");

        var employeeStub = scope.ServiceProvider.GetRequiredService<EmployeeSourceStub>();
        var costcenterStub = scope.ServiceProvider.GetRequiredService<CostcenterSourceStub>();
        var accessrightStub = scope.ServiceProvider.GetRequiredService<AccessrightSourceStub>();

        var (employees, costcenters, accessrights) = await FetchAllSourcesAsync(
            employeeStub, costcenterStub, accessrightStub, ct);

        var dataContext = new DataContext(employees, costcenters, accessrights);

        // ── Step 2: Load overrides and apply ────────────────────────────────────
        _logger.LogInformation("Step 2: Applying overrides.");

        var overrides = await db.CostcenterResponsibleOverrides.ToListAsync(ct);
        dataContext.ApplyOverrides(overrides);

        // Update the singleton cache for the web layer
        _cache.Latest = dataContext;

        // ── Step 3: Create PipelineRun record ───────────────────────────────────
        _logger.LogInformation("Step 3: Recording pipeline run.");

        // Need tracking for insert — use a new context options-scoped instance
        var trackedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var run = new PipelineRun
        {
            StartedAt = DateTime.UtcNow,
            Status = PipelineStatus.Running,
        };

        // We need tracking for this insert; temporarily enable it
        trackedDb.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.TrackAll;
        trackedDb.PipelineRuns.Add(run);
        await trackedDb.SaveChangesAsync(ct);

        // ── Step 4: Run all export jobs in parallel ──────────────────────────────
        _logger.LogInformation("Step 4: Running {Count} export job(s).", exportJobs.Count);

        var jobTasks = exportJobs.Select(job => job.RunAsync(dataContext, ct)).ToList();
        var jobResults = await Task.WhenAll(jobTasks);
        var results = jobResults.SelectMany(r => r).ToList();

        // ── Step 5: Write export logs, update pipeline run status ───────────────
        _logger.LogInformation("Step 5: Writing export logs.");

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count - successCount;

        run.FinishedAt = DateTime.UtcNow;
        run.Status = failureCount == 0
            ? PipelineStatus.Success
            : successCount == 0
                ? PipelineStatus.Failed
                : PipelineStatus.PartialFailure;

        foreach (var result in results.AsEnumerable())
        {
            var log = new ExportLog
            {
                PipelineRunId = run.Id,
                AppId = result.AppId,
                FileName = result.FileName,
                FileSizeBytes = result.FileSizeBytes,
                RecordCount = result.RecordCount,
                Status = result.Success ? ExportStatus.Success : ExportStatus.Failed,
                ErrorMessage = result.ErrorMessage,
                ExportedAt = DateTime.UtcNow,
            };
            trackedDb.ExportLogs.Add(log);
        }

        trackedDb.PipelineRuns.Update(run);
        await trackedDb.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Pipeline run {Id} finished with status {Status}. {Success} succeeded, {Failed} failed.",
            run.Id, run.Status, successCount, failureCount);
    }

    private static async Task<(List<Employee>, List<Costcenter>, List<Accessright>)> FetchAllSourcesAsync(
        EmployeeSourceStub empStub,
        CostcenterSourceStub ccStub,
        AccessrightSourceStub arStub,
        CancellationToken ct)
    {
        var empTask = empStub.FetchEmployeesAsync(ct);
        var ccTask  = ccStub.FetchCostcentersAsync(ct);
        var arTask  = arStub.FetchAccessrightsAsync(ct);

        await Task.WhenAll(empTask, ccTask, arTask);

        return (await empTask, await ccTask, await arTask);
    }

    /// <summary>Calculates the delay until the next 02:00 local time.</summary>
    private static TimeSpan ComputeDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var next = now.Date.AddHours(2); // today at 02:00

        if (next <= now)
            next = next.AddDays(1); // already past, schedule for tomorrow

        return next - now;
    }
}
