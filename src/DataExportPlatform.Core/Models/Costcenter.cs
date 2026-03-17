namespace DataExportPlatform.Core.Models;

public class Costcenter
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ResponsibleEmail { get; set; } = string.Empty; // mutable — overrides patch this
}
