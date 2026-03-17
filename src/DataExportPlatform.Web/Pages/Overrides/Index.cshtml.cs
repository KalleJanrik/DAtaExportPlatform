using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DataExportPlatform.Web.Pages.Overrides;

public class OverridesIndexModel : PageModel
{
    private readonly AppDbContext _db;

    public OverridesIndexModel(AppDbContext db) => _db = db;

    public List<CostcenterResponsibleOverride> Overrides { get; set; } = new();

    public async Task OnGetAsync()
    {
        Overrides = await _db.CostcenterResponsibleOverrides
            .OrderByDescending(o => o.ChangedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        // Need tracking for delete
        var db2 = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        db2.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.TrackAll;

        var entity = await db2.CostcenterResponsibleOverrides.FindAsync(id);
        if (entity is not null)
        {
            db2.CostcenterResponsibleOverrides.Remove(entity);
            await db2.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
