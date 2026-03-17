using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;

namespace DataExportPlatform.Pipeline.Validators;

public class CostcenterResponsibleValidator : IOverrideValidator<CostcenterResponsibleOverride>
{
    public Task<ValidationResult> ValidateAsync(CostcenterResponsibleOverride @override, DataContext ctx)
    {
        var errors = new List<string>();

        // Validate CostcenterId exists in ctx.Costcenters
        var costcenterExists = ctx.Costcenters.Any(c => c.Id == @override.CostcenterId);
        if (!costcenterExists)
        {
            errors.Add($"Costcenter with Id '{@override.CostcenterId}' does not exist.");
        }

        // Validate ResponsibleUserEmail exists as Email in ctx.Employees
        var emailExists = ctx.Employees.Any(e => e.Email.Equals(@override.ResponsibleUserEmail, StringComparison.OrdinalIgnoreCase));
        if (!emailExists)
        {
            errors.Add($"No employee found with email '{@override.ResponsibleUserEmail}'.");
        }

        var result = errors.Count == 0
            ? ValidationResult.Ok()
            : ValidationResult.Fail(errors.ToArray());

        return Task.FromResult(result);
    }
}
