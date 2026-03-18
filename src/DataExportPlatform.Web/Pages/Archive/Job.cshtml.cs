using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace DataExportPlatform.Web.Pages.Archive;

public class JobModel : PageModel
{
    private readonly string _archiveRoot;
    private readonly IAuthorizationService _authorizationService;

    public JobModel(IConfiguration configuration, IAuthorizationService authorizationService)
    {
        _archiveRoot = configuration["ExportSettings:ArchiveRoot"] ?? @"C:\DataExports\Archive";
        _authorizationService = authorizationService;
    }

    public string AppId { get; set; } = string.Empty;
    public List<DayGroup> Days { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string appId)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, null, $"Archive.{appId}");
        if (!auth.Succeeded)
            return Forbid();

        var jobDir = Path.Combine(_archiveRoot, appId);
        if (!Directory.Exists(jobDir))
            return NotFound();

        AppId = appId;

        Days = Directory
            .EnumerateDirectories(jobDir)
            .Where(d => DateTime.TryParse(Path.GetFileName(d), out _))
            .OrderByDescending(d => Path.GetFileName(d))
            .Select(d => new DayGroup
            {
                Day = Path.GetFileName(d),
                Files = Directory
                    .EnumerateFiles(d)
                    .Select(f => new ArchivedFile
                    {
                        FileName = Path.GetFileName(f),
                        SizeBytes = new FileInfo(f).Length,
                    })
                    .OrderBy(f => f.FileName)
                    .ToList(),
            })
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnGetDownloadAsync(string appId, string day, string file)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, null, $"Archive.{appId}");
        if (!auth.Succeeded)
            return Forbid();

        // Prevent path traversal
        if (appId.Contains('/') || appId.Contains('\\') ||
            day.Contains('/') || day.Contains('\\') ||
            file.Contains('/') || file.Contains('\\'))
            return BadRequest();

        var path = Path.Combine(_archiveRoot, appId, day, file);
        if (!System.IO.File.Exists(path))
            return NotFound();

        var stream = System.IO.File.OpenRead(path);
        var contentType = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? "application/json"
            : "text/csv";

        return File(stream, contentType, file);
    }
}

public class DayGroup
{
    public string Day { get; set; } = string.Empty;
    public List<ArchivedFile> Files { get; set; } = [];
}

public class ArchivedFile
{
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
