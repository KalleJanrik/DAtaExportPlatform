namespace DataExportPlatform.Infrastructure.FileWriting;

public class LocalFileWriter : IFileWriter
{
    public async Task WriteAsync(string directory, string filename, string content, CancellationToken ct = default)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, filename);
        await File.WriteAllTextAsync(path, content, ct);
    }
}
