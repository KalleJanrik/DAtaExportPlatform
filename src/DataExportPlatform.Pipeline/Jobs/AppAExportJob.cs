using CsvHelper;
using CsvHelper.Configuration;
using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.FileWriting;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;

namespace DataExportPlatform.Pipeline.Jobs;

public class AppAExportJob(IFileWriter fileWriter, IConfiguration configuration) : IExportJob
{
    private readonly IFileWriter _fileWriter = fileWriter;
    private readonly string _outputDirectory = configuration["ExportSettings:OutputDirectory"] ?? @"C:\DataExports";

    public string AppId => "AppA";

    public async Task<IEnumerable<ExportResult>> RunAsync(DataContext ctx, CancellationToken ct)
    {
        var date = DateTime.Today.ToString("yyyyMMdd");
        var fileName = $"appA_employees_{date}.csv";

        try
        {
            // Filter: active employees only
            var activeEmployees = ctx.Employees.Where(e => e.IsActive).ToList();

            // Build a lookup for costcenter responsible email
            var ccLookup = ctx.Costcenters.ToDictionary(c => c.Id, c => c);

            var records = activeEmployees.Select(e =>
            {
                ccLookup.TryGetValue(e.CostcenterId, out var cc);
                return new AppARecord
                {
                    EmployeeId = e.Id,
                    FullName = $"{e.FirstName} {e.LastName}",
                    CostcenterCode = cc?.Code ?? string.Empty,
                    ResponsibleEmail = cc?.ResponsibleEmail ?? string.Empty,
                };
            }).ToList();

            var csv = SerializeToCsv(records);
            await _fileWriter.WriteAsync(_outputDirectory, fileName, csv, ct);

            var fileSizeBytes = Encoding.UTF8.GetByteCount(csv);

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

    private static string SerializeToCsv(List<AppARecord> records)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using var sw = new StringWriter();
        using var csv = new CsvWriter(sw, config);
        csv.WriteRecords(records);
        return sw.ToString();
    }
}

public class AppARecord
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CostcenterCode { get; set; } = string.Empty;
    public string ResponsibleEmail { get; set; } = string.Empty;
}
