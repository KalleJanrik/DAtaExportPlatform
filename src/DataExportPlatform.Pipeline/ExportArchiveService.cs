using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataExportPlatform.Pipeline;

public class ExportArchiveService
{
    private readonly string _archiveRoot;
    private readonly int _defaultRetentionDays;
    // Key: lowercase filename prefix (e.g. "appa"), Value: retention days
    private readonly Dictionary<string, int> _jobRetention;
    private readonly ILogger<ExportArchiveService> _logger;

    public ExportArchiveService(IConfiguration configuration, ILogger<ExportArchiveService> logger)
    {
        _archiveRoot = configuration["ExportSettings:ArchiveRoot"] ?? @"C:\DataExports\Archive";
        _defaultRetentionDays = int.Parse(configuration["ExportSettings:RetentionDays"] ?? "30");
        _logger = logger;

        _jobRetention = configuration
            .GetSection("ExportSettings:JobRetention")
            .GetChildren()
            .ToDictionary(
                s => s.Key.ToLowerInvariant(),
                s => int.Parse(s.Value ?? _defaultRetentionDays.ToString()));
    }

    /// <summary>
    /// Purges day-folders from the archive that exceed the per-job (or global) retention window.
    /// Acts as a safety net for files the SFTP handler did not consume.
    /// </summary>
    public void PurgeExpiredArchive()
    {
        if (!Directory.Exists(_archiveRoot))
            return;

        var purged = 0;

        foreach (var jobDir in Directory.EnumerateDirectories(_archiveRoot))
        {
            var appId = Path.GetFileName(jobDir).ToLowerInvariant();
            var retention = _jobRetention.TryGetValue(appId, out var days) ? days : _defaultRetentionDays;
            var cutoff = DateTime.Today.AddDays(-retention);

            foreach (var dayDir in Directory.EnumerateDirectories(jobDir))
            {
                if (!DateTime.TryParse(Path.GetFileName(dayDir), out var dirDate))
                    continue;

                if (dirDate < cutoff)
                {
                    try
                    {
                        Directory.Delete(dayDir, recursive: true);
                        _logger.LogInformation(
                            "Purged expired archive folder: {AppId}/{Day} (retention: {Days} days)",
                            Path.GetFileName(jobDir), Path.GetFileName(dayDir), retention);
                        purged++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to purge archive folder: {Dir}", dayDir);
                    }
                }
            }
        }

        if (purged > 0)
            _logger.LogInformation("Archive purge complete. {Count} day-folder(s) removed.", purged);
        else
            _logger.LogInformation("Archive purge complete. No expired folders found.");
    }

}
