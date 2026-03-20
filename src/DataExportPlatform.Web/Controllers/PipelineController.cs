using DataExportPlatform.Pipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Authorize]
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
    /// Runs the pipeline synchronously. Returns 200 on success; exceptions bubble to UseExceptionHandler().
    /// Designed to be called by an external scheduler (Task Scheduler, cron, Azure Logic Apps, etc.).
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        _logger.LogInformation("Pipeline trigger requested.");
        await _orchestrator.RunAsync(ct);
        return Ok(new { message = "Pipeline run completed successfully." });
    }
}
