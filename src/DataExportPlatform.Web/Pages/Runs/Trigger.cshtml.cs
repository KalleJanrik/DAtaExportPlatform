using DataExportPlatform.Pipeline;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataExportPlatform.Web.Pages.Runs;

public class TriggerModel : PageModel
{
    private readonly PipelineOrchestrator _orchestrator;

    public TriggerModel(PipelineOrchestrator orchestrator) => _orchestrator = orchestrator;

    public bool Triggered { get; set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await _orchestrator.TriggerManualRunAsync(ct);
        Triggered = true;
        return Page();
    }
}
