namespace DataExportPlatform.Core.Models;

public class ExportLog
{
    public int Id { get; set; }
    public int PipelineRunId { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int RecordCount { get; set; }
    public ExportStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExportedAt { get; set; }

    public PipelineRun? PipelineRun { get; set; }
}

public enum ExportStatus
{
    Success,
    Failed
}
