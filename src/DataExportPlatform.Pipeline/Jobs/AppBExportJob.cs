using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.FileWriting;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace DataExportPlatform.Pipeline.Jobs;

public class AppBExportJob(IFileWriter fileWriter, IConfiguration configuration) : IExportJob
{
    private readonly IFileWriter _fileWriter = fileWriter;
    private readonly string _outputDirectory = configuration["ExportSettings:OutputDirectory"] ?? @"C:\DataExports";

    public string AppId => "AppB";

    public async Task<IEnumerable<ExportResult>> RunAsync(DataContext ctx, CancellationToken ct)
    {
        var date = DateTime.Today.ToString("yyyyMMdd");
        var fileName = $"appB_accessrights_{date}.json";

        try
        {
            // Build employee lookup for name resolution
            var employeeLookup = ctx.Employees.ToDictionary(e => e.Id, e => e);

            // Filter: all accessrights joined with employee name
            var records = ctx.Accessrights.Select(ar =>
            {
                employeeLookup.TryGetValue(ar.EmployeeId, out var emp);
                return new AppBRecord
                {
                    AccessrightCode = ar.Code,
                    EmployeeId = ar.EmployeeId,
                    EmployeeFullName = emp is not null ? $"{emp.FirstName} {emp.LastName}" : "Unknown",
                    Description = ar.Description,
                };
            }).ToList();

            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            await _fileWriter.WriteAsync(_outputDirectory, fileName, json, ct);

            var fileSizeBytes = Encoding.UTF8.GetByteCount(json);

            return [new ExportResult
            {
                AppId = AppId,
                FileName = fileName,
                FileSizeBytes = fileSizeBytes,
                RecordCount = records.Count,
                Success = true,
            }];
        }
        catch (Exception ex)
        {
            return [new ExportResult
            {
                AppId = AppId,
                FileName = fileName,
                FileSizeBytes = 0,
                RecordCount = 0,
                Success = false,
                ErrorMessage = ex.Message,
            }];
        }
    }
}

public class AppBRecord
{
    public string AccessrightCode { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
