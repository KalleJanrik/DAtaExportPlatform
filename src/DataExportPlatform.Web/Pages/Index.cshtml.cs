using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DataExportPlatform.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<PipelineRun> Runs { get; set; } = new();

    public async Task OnGetAsync()
    {
        Runs = await _db.PipelineRuns
            .Include(r => r.ExportLogs)
            .OrderByDescending(r => r.StartedAt)
            .Take(10)
            .ToListAsync();
    }
}
