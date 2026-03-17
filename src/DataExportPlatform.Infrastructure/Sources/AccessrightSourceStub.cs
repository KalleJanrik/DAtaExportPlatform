// STUB — replace with real access-right data source
using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;

namespace DataExportPlatform.Infrastructure.Sources;

public class AccessrightSourceStub : IDataSource
{
    public Task<SourceResult> FetchAsync(CancellationToken ct)
    {
        // TODO: Replace with real access-right data source (IAM / AD, etc.)
        return Task.FromResult(new SourceResult
        {
            SourceName = nameof(AccessrightSourceStub),
            RecordCount = 12,
            Success = true,
        });
    }

    public Task<List<Accessright>> FetchAccessrightsAsync(CancellationToken ct)
    {
        // TODO: Replace with real access-right data source (IAM / Active Directory, etc.)
        var accessrights = new List<Accessright>
        {
            new() { Id = 1,  Code = "AR_READ_FINANCE",    Description = "Read access to Finance module",       EmployeeId = 1  },
            new() { Id = 2,  Code = "AR_WRITE_FINANCE",   Description = "Write access to Finance module",      EmployeeId = 8  },
            new() { Id = 3,  Code = "AR_READ_HR",         Description = "Read access to HR module",            EmployeeId = 2  },
            new() { Id = 4,  Code = "AR_WRITE_HR",        Description = "Write access to HR module",           EmployeeId = 11 },
            new() { Id = 5,  Code = "AR_ADMIN_IT",        Description = "IT system administrator",             EmployeeId = 6  },
            new() { Id = 6,  Code = "AR_READ_REPORTS",    Description = "Access to reporting dashboard",       EmployeeId = 5  },
            new() { Id = 7,  Code = "AR_WRITE_CRM",       Description = "Write access to CRM system",         EmployeeId = 9  },
            new() { Id = 8,  Code = "AR_READ_CRM",        Description = "Read access to CRM system",          EmployeeId = 13 },
            new() { Id = 9,  Code = "AR_DEPLOY_PROD",     Description = "Production deployment rights",       EmployeeId = 4  },
            new() { Id = 10, Code = "AR_READ_AUDIT",      Description = "Read access to audit logs",          EmployeeId = 10 },
            new() { Id = 11, Code = "AR_MANAGE_USERS",    Description = "User management rights",             EmployeeId = 14 },
            new() { Id = 12, Code = "AR_READ_SALES",      Description = "Read access to sales data",          EmployeeId = 15 },
        };
        return Task.FromResult(accessrights);
    }
}
