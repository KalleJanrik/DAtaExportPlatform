namespace DataExportPlatform.Core.Models;

public class SourceResult
{
    public string SourceName { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
