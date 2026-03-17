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

    /// <summary>Triggers a manual pipeline run. Returns 202 Accepted immediately.</summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        _logger.LogInformation("Manual pipeline trigger requested.");
        await _orchestrator.TriggerManualRunAsync(ct);
        return Accepted(new { message = "Pipeline run triggered." });
    }
}
