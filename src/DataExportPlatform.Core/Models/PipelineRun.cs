namespace DataExportPlatform.Core.Models;

public class PipelineRun
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public PipelineStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    public List<ExportLog> ExportLogs { get; set; } = new();
}

public enum PipelineStatus
{
    Running,
    Success,
    PartialFailure,
    Failed
}
