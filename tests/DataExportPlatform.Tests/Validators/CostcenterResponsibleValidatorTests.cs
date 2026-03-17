using DataExportPlatform.Core.Models;
using DataExportPlatform.Pipeline.Validators;
using Xunit;

namespace DataExportPlatform.Tests.Validators;

public class CostcenterResponsibleValidatorTests
{
    private static DataContext BuildContext() => new(
        employees: new List<Employee>
        {
            new() { Id = 1, Email = "alice@example.com", IsActive = true, FirstName = "Alice", LastName = "A", DepartmentCode = "IT", CostcenterId = 1 }
        },
        costcenters: new List<Costcenter>
        {
            new() { Id = 1, Code = "CC001", Name = "IT", ResponsibleEmail = "manager@example.com" }
        },
        accessrights: Array.Empty<Accessright>());

    [Fact]
    public async Task ValidOverride_ReturnsValid()
    {
        var validator = new CostcenterResponsibleValidator();
        var ctx = BuildContext();
        var @override = new CostcenterResponsibleOverride
        {
            CostcenterId = 1,
            ResponsibleUserEmail = "alice@example.com",
            ChangedBy = "test",
            ChangedAt = DateTime.UtcNow,
        };

        var result = await validator.ValidateAsync(@override, ctx);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UnknownCostcenter_ReturnsError()
    {
        var validator = new CostcenterResponsibleValidator();
        var ctx = BuildContext();
        var @override = new CostcenterResponsibleOverride
        {
            CostcenterId = 999,
            ResponsibleUserEmail = "alice@example.com",
            ChangedBy = "test",
            ChangedAt = DateTime.UtcNow,
        };

        var result = await validator.ValidateAsync(@override, ctx);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("999"));
    }

    [Fact]
    public async Task UnknownEmail_ReturnsError()
    {
        var validator = new CostcenterResponsibleValidator();
        var ctx = BuildContext();
        var @override = new CostcenterResponsibleOverride
        {
            CostcenterId = 1,
            ResponsibleUserEmail = "nobody@example.com",
            ChangedBy = "test",
            ChangedAt = DateTime.UtcNow,
        };

        var result = await validator.ValidateAsync(@override, ctx);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("nobody@example.com"));
    }
}
