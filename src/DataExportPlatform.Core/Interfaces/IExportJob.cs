using DataExportPlatform.Core.Models;

namespace DataExportPlatform.Core.Interfaces;

public interface IExportJob
{
    string AppId { get; }
    Task<IEnumerable<ExportResult>> RunAsync(DataContext ctx, CancellationToken ct);
}
