using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/archive")]
public class ArchiveController : ControllerBase
{
    private static readonly string[] KnownAppIds = ["AppA", "AppB", "AppC", "AppD"];

    private readonly string _archiveRoot;
    private readonly IAuthorizationService _authorizationService;

    public ArchiveController(IConfiguration configuration, IAuthorizationService authorizationService)
    {
        _archiveRoot = Path.GetFullPath(configuration["ExportSettings:ArchiveRoot"] ?? @"C:\DataExports\Archive");
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummaries()
    {
        var summaries = new List<ArchiveSummaryDto>();

        foreach (var appId in KnownAppIds)
        {
            var auth = await _authorizationService.AuthorizeAsync(User, null, $"Archive.{appId}");
            if (!auth.Succeeded)
                continue;

            var jobDir = Path.Combine(_archiveRoot, appId);
            if (!Directory.Exists(jobDir))
                continue;

            var dayDirs = Directory.EnumerateDirectories(jobDir).ToList();
            var fileCount = dayDirs.Sum(d => Directory.EnumerateFiles(d).Count());
            var latestDay = dayDirs
                .Select(d => Path.GetFileName(d))
                .Where(n => DateTime.TryParse(n, out _))
                .OrderDescending()
                .FirstOrDefault();

            summaries.Add(new ArchiveSummaryDto(appId, dayDirs.Count, fileCount, latestDay));
        }

        return Ok(summaries);
    }

    [HttpGet("{appId}")]
    public async Task<IActionResult> GetJob(string appId)
    {
        if (!IsKnownAppId(appId))
            return NotFound();

        var auth = await _authorizationService.AuthorizeAsync(User, null, $"Archive.{appId}");
        if (!auth.Succeeded)
            return Forbid();

        var jobDir = Path.Combine(_archiveRoot, appId);
        if (!Directory.Exists(jobDir))
            return NotFound();

        var days = Directory
            .EnumerateDirectories(jobDir)
            .Where(d => DateTime.TryParse(Path.GetFileName(d), out _))
            .OrderByDescending(d => Path.GetFileName(d))
            .Select(d => new DayGroupDto(
                Path.GetFileName(d),
                Directory.EnumerateFiles(d)
                    .Select(f => new ArchivedFileDto(Path.GetFileName(f), new FileInfo(f).Length))
                    .OrderBy(f => f.FileName)
                    .ToList()))
            .ToList();

        return Ok(new ArchiveJobDto(appId, days));
    }

    [HttpGet("{appId}/{day}/{file}")]
    public async Task<IActionResult> Download(string appId, string day, string file)
    {
        if (!IsKnownAppId(appId))
            return NotFound();

        // Path traversal guard — segment-level checks
        if (IsUnsafeSegment(day) || IsUnsafeSegment(file))
            return BadRequest();

        var auth = await _authorizationService.AuthorizeAsync(User, null, $"Archive.{appId}");
        if (!auth.Succeeded)
            return Forbid();

        var path = Path.Combine(_archiveRoot, appId, day, file);

        // Canonical-path containment check — defence-in-depth after OS normalisation
        if (!Path.GetFullPath(path).StartsWith(_archiveRoot, StringComparison.OrdinalIgnoreCase))
            return BadRequest();

        if (!System.IO.File.Exists(path))
            return NotFound();

        var stream = System.IO.File.OpenRead(path);
        var contentType = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? "application/json"
            : path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? "text/csv"
            : "application/octet-stream";

        return File(stream, contentType, file);
    }

    private static bool IsKnownAppId(string appId) =>
        KnownAppIds.Contains(appId, StringComparer.OrdinalIgnoreCase);

    private static bool IsUnsafeSegment(string s) =>
        s.Contains('/') || s.Contains('\\') || s.Contains("..") || s.Contains('%');
}
