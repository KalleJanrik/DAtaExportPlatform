using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.Data;
using DataExportPlatform.Pipeline;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DataExportPlatform.Web.Pages.Overrides;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly DataContextCache _cache;
    private readonly IOverrideValidator<CostcenterResponsibleOverride> _validator;

    public EditModel(
        AppDbContext db,
        DataContextCache cache,
        IOverrideValidator<CostcenterResponsibleOverride> validator)
    {
        _db = db;
        _cache = cache;
        _validator = validator;
    }

    [BindProperty]
    public CostcenterResponsibleOverride Override { get; set; } = new();

    public List<Costcenter> AvailableCostcenters { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
    public bool IsNew => Override.Id == 0;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        PopulateCostcenters();

        if (id.HasValue)
        {
            var existing = await _db.CostcenterResponsibleOverrides.FindAsync(id.Value);
            if (existing is null)
                return NotFound();
            Override = existing;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        PopulateCostcenters();

        if (!ModelState.IsValid)
            return Page();

        // Run server-side validation using the DataContext cache
        var ctx = _cache.Latest;
        if (ctx is not null)
        {
            var validationResult = await _validator.ValidateAsync(Override, ctx);
            if (!validationResult.IsValid)
            {
                ValidationErrors = validationResult.Errors;
                return Page();
            }
        }
        // If cache is not yet populated, skip validation (first run)

        Override.ChangedAt = DateTime.UtcNow;
        Override.ChangedBy = User.Identity?.Name ?? "system"; // TODO: use real identity when auth is added

        // Need tracking for upsert
        _db.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.TrackAll;

        if (Override.Id == 0)
        {
            _db.CostcenterResponsibleOverrides.Add(Override);
        }
        else
        {
            _db.CostcenterResponsibleOverrides.Update(Override);
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/Overrides/Index");
    }

    private void PopulateCostcenters()
    {
        // Populate from DataContextCache (populated by PipelineOrchestrator)
        // Falls back to empty list if no run has completed yet.
        AvailableCostcenters = _cache.Latest?.Costcenters.ToList() ?? new List<Costcenter>();
    }
}
