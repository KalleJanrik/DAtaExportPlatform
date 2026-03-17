using DataExportPlatform.Core.Models;

namespace DataExportPlatform.Core.Interfaces;

public interface IOverrideValidator<T>
{
    Task<ValidationResult> ValidateAsync(T @override, DataContext ctx);
}
