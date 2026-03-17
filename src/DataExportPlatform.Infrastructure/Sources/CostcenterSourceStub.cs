// STUB — replace with real cost-center data source
using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;

namespace DataExportPlatform.Infrastructure.Sources;

public class CostcenterSourceStub : IDataSource
{
    public Task<SourceResult> FetchAsync(CancellationToken ct)
    {
        // TODO: Replace with real cost-center data source (ERP, Finance system, etc.)
        return Task.FromResult(new SourceResult
        {
            SourceName = nameof(CostcenterSourceStub),
            RecordCount = 10,
            Success = true,
        });
    }

    public Task<List<Costcenter>> FetchCostcentersAsync(CancellationToken ct)
    {
        // TODO: Replace with real cost-center data source (ERP, Finance system, etc.)
        var costcenters = new List<Costcenter>
        {
            new() { Id = 1,  Code = "CC001", Name = "Information Technology",  ResponsibleEmail = "it.manager@example.com"  },
            new() { Id = 2,  Code = "CC002", Name = "Human Resources",          ResponsibleEmail = "hr.manager@example.com"  },
            new() { Id = 3,  Code = "CC003", Name = "Finance",                  ResponsibleEmail = "fin.manager@example.com" },
            new() { Id = 4,  Code = "CC004", Name = "Marketing",                ResponsibleEmail = "mkt.manager@example.com" },
            new() { Id = 5,  Code = "CC005", Name = "Sales",                    ResponsibleEmail = "sales.manager@example.com" },
            new() { Id = 6,  Code = "CC006", Name = "Operations",               ResponsibleEmail = "ops.manager@example.com" },
            new() { Id = 7,  Code = "CC007", Name = "Legal",                    ResponsibleEmail = "legal.manager@example.com" },
            new() { Id = 8,  Code = "CC008", Name = "Customer Support",         ResponsibleEmail = "support.manager@example.com" },
            new() { Id = 9,  Code = "CC009", Name = "Research & Development",   ResponsibleEmail = "rd.manager@example.com" },
            new() { Id = 10, Code = "CC010", Name = "Executive",                ResponsibleEmail = "exec@example.com" },
        };
        return Task.FromResult(costcenters);
    }
}
