using DataExportPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/runs")]
public class RunsController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> GetRuns()
    {
        var runs = await _db.PipelineRuns
            .Include(r => r.ExportLogs)
            .OrderByDescending(r => r.StartedAt)
            .Take(10)
            .ToListAsync();

        var dtos = runs.Select(r => new PipelineRunSummaryDto(
            r.Id,
            r.StartedAt,
            r.FinishedAt,
            r.Status.ToString(),
            r.ErrorMessage,
            r.ExportLogs.Select(l => new ExportLogDto(
                l.AppId, l.FileName, l.RecordCount, l.FileSizeBytes,
                l.Status.ToString(), l.ExportedAt, l.ErrorMessage)).ToList()));

        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRun(int id)
    {
        var run = await _db.PipelineRuns
            .Include(r => r.ExportLogs)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (run is null)
            return NotFound();

        var dto = new PipelineRunSummaryDto(
            run.Id,
            run.StartedAt,
            run.FinishedAt,
            run.Status.ToString(),
            run.ErrorMessage,
            run.ExportLogs.Select(l => new ExportLogDto(
                l.AppId, l.FileName, l.RecordCount, l.FileSizeBytes,
                l.Status.ToString(), l.ExportedAt, l.ErrorMessage)).ToList());

        return Ok(dto);
    }
}
