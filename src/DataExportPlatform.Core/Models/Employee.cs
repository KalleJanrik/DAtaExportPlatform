namespace DataExportPlatform.Core.Models;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public int CostcenterId { get; set; }
}
