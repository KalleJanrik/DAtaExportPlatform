// STUB — replace with real HR system / API call
using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;

namespace DataExportPlatform.Infrastructure.Sources;

public class EmployeeSourceStub : IDataSource
{
    public Task<SourceResult> FetchAsync(CancellationToken ct)
    {
        // TODO: Replace with real employee data source (HR API, LDAP, etc.)
        var employees = new List<Employee>
        {
            new() { Id = 1,  FirstName = "Alice",   LastName = "Anderson",  Email = "alice.anderson@example.com",   IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 2,  FirstName = "Bob",     LastName = "Baker",     Email = "bob.baker@example.com",        IsActive = true,  DepartmentCode = "HR",  CostcenterId = 2 },
            new() { Id = 3,  FirstName = "Carol",   LastName = "Clark",     Email = "carol.clark@example.com",      IsActive = false, DepartmentCode = "FIN", CostcenterId = 3 },
            new() { Id = 4,  FirstName = "David",   LastName = "Davis",     Email = "david.davis@example.com",      IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 5,  FirstName = "Eve",     LastName = "Evans",     Email = "eve.evans@example.com",        IsActive = true,  DepartmentCode = "MKT", CostcenterId = 4 },
            new() { Id = 6,  FirstName = "Frank",   LastName = "Foster",    Email = "frank.foster@example.com",     IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 7,  FirstName = "Grace",   LastName = "Green",     Email = "grace.green@example.com",      IsActive = false, DepartmentCode = "HR",  CostcenterId = 2 },
            new() { Id = 8,  FirstName = "Henry",   LastName = "Harris",    Email = "henry.harris@example.com",     IsActive = true,  DepartmentCode = "FIN", CostcenterId = 3 },
            new() { Id = 9,  FirstName = "Iris",    LastName = "Ingram",    Email = "iris.ingram@example.com",      IsActive = true,  DepartmentCode = "MKT", CostcenterId = 4 },
            new() { Id = 10, FirstName = "Jack",    LastName = "Jackson",   Email = "jack.jackson@example.com",     IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 11, FirstName = "Karen",   LastName = "King",      Email = "karen.king@example.com",       IsActive = true,  DepartmentCode = "HR",  CostcenterId = 2 },
            new() { Id = 12, FirstName = "Liam",    LastName = "Lewis",     Email = "liam.lewis@example.com",       IsActive = false, DepartmentCode = "FIN", CostcenterId = 3 },
            new() { Id = 13, FirstName = "Mia",     LastName = "Moore",     Email = "mia.moore@example.com",        IsActive = true,  DepartmentCode = "MKT", CostcenterId = 4 },
            new() { Id = 14, FirstName = "Noah",    LastName = "Nelson",    Email = "noah.nelson@example.com",      IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 15, FirstName = "Olivia",  LastName = "Owen",      Email = "olivia.owen@example.com",      IsActive = true,  DepartmentCode = "HR",  CostcenterId = 2 },
        };

        return Task.FromResult(new SourceResult
        {
            SourceName = nameof(EmployeeSourceStub),
            RecordCount = employees.Count,
            Success = true,
            // Attach data via a side-channel; real pattern would use typed results.
            // See DataContextBuilder for how this stub is consumed.
        });
    }

    /// <summary>Returns the stub employee list directly for use by DataContextBuilder.</summary>
    public Task<List<Employee>> FetchEmployeesAsync(CancellationToken ct)
    {
        // TODO: Replace with real employee data source (HR API, LDAP, etc.)
        var employees = new List<Employee>
        {
            new() { Id = 1,  FirstName = "Alice",   LastName = "Anderson",  Email = "alice.anderson@example.com",   IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 2,  FirstName = "Bob",     LastName = "Baker",     Email = "bob.baker@example.com",        IsActive = true,  DepartmentCode = "HR",  CostcenterId = 2 },
            new() { Id = 3,  FirstName = "Carol",   LastName = "Clark",     Email = "carol.clark@example.com",      IsActive = false, DepartmentCode = "FIN", CostcenterId = 3 },
            new() { Id = 4,  FirstName = "David",   LastName = "Davis",     Email = "david.davis@example.com",      IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 5,  FirstName = "Eve",     LastName = "Evans",     Email = "eve.evans@example.com",        IsActive = true,  DepartmentCode = "MKT", CostcenterId = 4 },
            new() { Id = 6,  FirstName = "Frank",   LastName = "Foster",    Email = "frank.foster@example.com",     IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 7,  FirstName = "Grace",   LastName = "Green",     Email = "grace.green@example.com",      IsActive = false, DepartmentCode = "HR",  CostcenterId = 2 },
            new() { Id = 8,  FirstName = "Henry",   LastName = "Harris",    Email = "henry.harris@example.com",     IsActive = true,  DepartmentCode = "FIN", CostcenterId = 3 },
            new() { Id = 9,  FirstName = "Iris",    LastName = "Ingram",    Email = "iris.ingram@example.com",      IsActive = true,  DepartmentCode = "MKT", CostcenterId = 4 },
            new() { Id = 10, FirstName = "Jack",    LastName = "Jackson",   Email = "jack.jackson@example.com",     IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 11, FirstName = "Karen",   LastName = "King",      Email = "karen.king@example.com",       IsActive = true,  DepartmentCode = "HR",  CostcenterId = 2 },
            new() { Id = 12, FirstName = "Liam",    LastName = "Lewis",     Email = "liam.lewis@example.com",       IsActive = false, DepartmentCode = "FIN", CostcenterId = 3 },
            new() { Id = 13, FirstName = "Mia",     LastName = "Moore",     Email = "mia.moore@example.com",        IsActive = true,  DepartmentCode = "MKT", CostcenterId = 4 },
            new() { Id = 14, FirstName = "Noah",    LastName = "Nelson",    Email = "noah.nelson@example.com",      IsActive = true,  DepartmentCode = "IT",  CostcenterId = 1 },
            new() { Id = 15, FirstName = "Olivia",  LastName = "Owen",      Email = "olivia.owen@example.com",      IsActive = true,  DepartmentCode = "HR",  CostcenterId = 2 },
        };
        return Task.FromResult(employees);
    }
}
