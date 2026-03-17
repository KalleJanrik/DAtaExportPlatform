using DataExportPlatform.Core.Models;

namespace DataExportPlatform.Core.Interfaces;

public interface IDataSource
{
    Task<SourceResult> FetchAsync(CancellationToken ct);
}
