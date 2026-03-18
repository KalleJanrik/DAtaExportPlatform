using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace DataExportPlatform.Web.Pages.Archive;

public class IndexModel : PageModel
{
    private readonly string _archiveRoot;

    public IndexModel(IConfiguration configuration)
    {
        _archiveRoot = configuration["ExportSettings:ArchiveRoot"] ?? @"C:\DataExports\Archive";
    }

    public List<JobSummary> Jobs { get; set; } = [];

    public void OnGet()
    {
        if (!Directory.Exists(_archiveRoot))
            return;

        Jobs = Directory
            .EnumerateDirectories(_archiveRoot)
            .Select(dir =>
            {
                var dayDirs = Directory.EnumerateDirectories(dir).ToList();
                var fileCount = dayDirs.Sum(d => Directory.EnumerateFiles(d).Count());
                var latest = dayDirs
                    .Select(d => Path.GetFileName(d))
                    .Where(n => DateTime.TryParse(n, out _))
                    .OrderDescending()
                    .FirstOrDefault();

                return new JobSummary
                {
                    AppId = Path.GetFileName(dir),
                    DayCount = dayDirs.Count,
                    FileCount = fileCount,
                    LatestDay = latest,
                };
            })
            .OrderBy(j => j.AppId)
            .ToList();
    }
}

public class JobSummary
{
    public string AppId { get; set; } = string.Empty;
    public int DayCount { get; set; }
    public int FileCount { get; set; }
    public string? LatestDay { get; set; }
}
