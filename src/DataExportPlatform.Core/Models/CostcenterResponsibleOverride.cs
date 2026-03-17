namespace DataExportPlatform.Core.Models;

public class CostcenterResponsibleOverride
{
    public int Id { get; set; }
    public int CostcenterId { get; set; }
    public string ResponsibleUserEmail { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}
