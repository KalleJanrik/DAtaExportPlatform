using Microsoft.Extensions.Configuration;

namespace DataExportPlatform.Infrastructure.FileWriting;

/// <summary>
/// Writes each file to two destinations in parallel:
///   1. The SFTP drop directory (the directory argument passed by the job).
///   2. The archive: {ArchiveRoot}\{AppId}\{yyyy-MM-dd}\{filename}
/// The AppId is derived from the filename prefix convention (e.g. "appA_" → "AppA").
/// </summary>
public class ArchivingFileWriter : IFileWriter
{
    private readonly LocalFileWriter _inner;
    private readonly string _archiveRoot;

    public ArchivingFileWriter(IConfiguration configuration)
    {
        _inner = new LocalFileWriter();
        _archiveRoot = configuration["ExportSettings:ArchiveRoot"] ?? @"C:\DataExports\Archive";
    }

    public async Task WriteAsync(string directory, string filename, string content, CancellationToken ct = default)
    {
        var archiveDirectory = BuildArchivePath(filename);

        await Task.WhenAll(
            _inner.WriteAsync(directory, filename, content, ct),
            _inner.WriteAsync(archiveDirectory, filename, content, ct));
    }

    private string BuildArchivePath(string filename)
    {
        var appId = ExtractAppId(filename);
        var day = DateTime.Today.ToString("yyyy-MM-dd");
        return Path.Combine(_archiveRoot, appId, day);
    }

    private static string ExtractAppId(string filename)
    {
        var prefix = filename.Split('_')[0];
        return char.ToUpperInvariant(prefix[0]) + prefix[1..];
    }
}
