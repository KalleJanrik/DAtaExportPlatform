using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DataExportPlatform.Web.Pages.Runs;

public class DetailModel : PageModel
{
    private readonly AppDbContext _db;

    public DetailModel(AppDbContext db) => _db = db;

    public PipelineRun? Run { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Run = await _db.PipelineRuns
            .Include(r => r.ExportLogs)
            .FirstOrDefaultAsync(r => r.Id == id);

        return Page();
    }
}
