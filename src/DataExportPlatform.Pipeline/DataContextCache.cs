using DataExportPlatform.Core.Models;

namespace DataExportPlatform.Pipeline;

/// <summary>
/// Singleton cache that holds the most recently fetched DataContext.
/// Updated by PipelineOrchestrator after each successful fetch step.
/// </summary>
public class DataContextCache
{
    private volatile DataContext? _latest;

    public DataContext? Latest
    {
        get => _latest;
        set => _latest = value;
    }
}
