using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.Data;
using DataExportPlatform.Infrastructure.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataExportPlatform.Pipeline;

public class PipelineOrchestrator
{
    private readonly IServiceProvider _services;
    private readonly DataContextCache _cache;
    private readonly ExportArchiveService _archive;
    private readonly ILogger<PipelineOrchestrator> _logger;

    public PipelineOrchestrator(
        IServiceProvider services,
        DataContextCache cache,
        ExportArchiveService archive,
        ILogger<PipelineOrchestrator> logger)
    {
        _services = services;
        _cache = cache;
        _archive = archive;
        _logger = logger;
    }

    /// <summary>Runs the full pipeline and returns when complete.</summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Pipeline run starting.");
        try
        {
            await RunPipelineAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in pipeline run.");
            throw;
        }
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

        // ── Step 6: Purge archive day-folders that exceed retention ─────────────
        _logger.LogInformation("Step 6: Purging expired archive folders.");
        _archive.PurgeExpiredArchive();
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

}
