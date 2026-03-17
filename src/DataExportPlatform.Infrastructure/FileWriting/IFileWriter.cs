namespace DataExportPlatform.Infrastructure.FileWriting;

public interface IFileWriter
{
    Task WriteAsync(string directory, string filename, string content, CancellationToken ct = default);
}
