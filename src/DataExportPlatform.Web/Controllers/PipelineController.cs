using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Pipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/pipeline")]
public class PipelineController(
    PipelineOrchestrator orchestrator,
    ILogger<PipelineController> logger,
    IEnumerable<IExportJob> jobs) : ControllerBase
{
    private readonly PipelineOrchestrator _orchestrator = orchestrator;
    private readonly ILogger<PipelineController> _logger = logger;
    private readonly IReadOnlySet<string> _availableJobIds =
        jobs.Select(j => j.AppId).ToHashSet();

    [HttpGet("jobs")]
    public IActionResult GetJobs()
    {
        return Ok(_availableJobIds.Select(id => new { appId = id }));
    }

    /// <summary>
    /// Runs the pipeline synchronously for the specified jobs. Returns 200 on success; exceptions bubble to UseExceptionHandler().
    /// Designed to be called by an external scheduler (Task Scheduler, cron, Azure Logic Apps, etc.).
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger([FromBody] TriggerRequest request, CancellationToken ct)
    {
        if (request.Jobs is null || request.Jobs.Count == 0)
            return BadRequest(new { message = "At least one job must be selected." });

        var unknown = request.Jobs.Except(_availableJobIds).ToList();
        if (unknown.Count > 0)
            return BadRequest(new { message = $"Unknown job IDs: {string.Join(", ", unknown)}" });

        _logger.LogInformation("Pipeline trigger requested for jobs: {Jobs}.", string.Join(", ", request.Jobs));
        await _orchestrator.RunAsync(request.Jobs, ct);
        return Ok(new { message = "Pipeline run completed successfully." });
    }
}

public record TriggerRequest(List<string>? Jobs);
