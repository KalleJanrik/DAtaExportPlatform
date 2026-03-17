using DataExportPlatform.Core.Models;
using Xunit;

namespace DataExportPlatform.Tests.Models;

public class DataContextTests
{
    [Fact]
    public void ApplyOverrides_MutatesMatchingCostcenters()
    {
        var costcenters = new List<Costcenter>
        {
            new() { Id = 1, Code = "CC1", Name = "IT", ResponsibleEmail = "original@example.com" },
            new() { Id = 2, Code = "CC2", Name = "HR", ResponsibleEmail = "hr@example.com" },
        };

        var ctx = new DataContext(
            employees: Array.Empty<Employee>(),
            costcenters: costcenters,
            accessrights: Array.Empty<Accessright>());

        var overrides = new[]
        {
            new CostcenterResponsibleOverride { CostcenterId = 1, ResponsibleUserEmail = "new@example.com" }
        };

        var result = ctx.ApplyOverrides(overrides);

        Assert.Same(ctx, result); // returns same instance
        Assert.Equal("new@example.com", costcenters[0].ResponsibleEmail);
        Assert.Equal("hr@example.com",  costcenters[1].ResponsibleEmail); // unchanged
    }
}
