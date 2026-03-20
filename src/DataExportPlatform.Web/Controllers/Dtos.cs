namespace DataExportPlatform.Web.Controllers;

// ── Runs ──────────────────────────────────────────────────────────────────────

public record PipelineRunSummaryDto(
    int Id,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string Status,
    string? ErrorMessage,
    IEnumerable<ExportLogDto> ExportLogs);

public record ExportLogDto(
    string AppId,
    string FileName,
    int RecordCount,
    long FileSizeBytes,
    string Status,
    DateTime ExportedAt,
    string? ErrorMessage);

// ── Archive ───────────────────────────────────────────────────────────────────

public record ArchiveSummaryDto(string AppId, int DayCount, int FileCount, string? LatestDay);

public record ArchiveJobDto(string AppId, IEnumerable<DayGroupDto> Days);

public record DayGroupDto(string Day, IEnumerable<ArchivedFileDto> Files);

public record ArchivedFileDto(string FileName, long SizeBytes);
