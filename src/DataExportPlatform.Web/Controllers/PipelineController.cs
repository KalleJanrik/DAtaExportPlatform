using DataExportPlatform.Pipeline;
using Microsoft.AspNetCore.Mvc;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Route("api/pipeline")]
public class PipelineController : ControllerBase
{
    private readonly PipelineOrchestrator _orchestrator;
    private readonly ILogger<PipelineController> _logger;

    public PipelineController(PipelineOrchestrator orchestrator, ILogger<PipelineController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Runs the pipeline synchronously and returns 200 OK when complete, or 500 on failure.
    /// Designed to be called by an external scheduler (Task Scheduler, cron, Azure Logic Apps, etc.).
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        _logger.LogInformation("Pipeline trigger requested.");
        try
        {
            await _orchestrator.RunAsync(ct);
            return Ok(new { message = "Pipeline run completed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline run failed.");
            return StatusCode(500, new { message = "Pipeline run failed.", error = ex.Message });
        }
    }
}
