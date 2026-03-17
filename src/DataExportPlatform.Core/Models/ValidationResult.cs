namespace DataExportPlatform.Core.Models;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();

    public static ValidationResult Ok() => new();

    public static ValidationResult Fail(params string[] errors) =>
        new() { Errors = errors.ToList() };
}
