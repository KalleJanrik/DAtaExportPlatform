using CsvHelper;
using CsvHelper.Configuration;
using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.FileWriting;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;

namespace DataExportPlatform.Pipeline.Jobs;

public class AppDExportJob(IFileWriter fileWriter, IConfiguration configuration) : IExportJob
{
    private readonly IFileWriter _fileWriter = fileWriter;
    private readonly string _outputDirectory = configuration["ExportSettings:OutputDirectory"] ?? @"C:\DataExports";
    private readonly string _sourceFilePath = configuration["ExportSettings:AppDSourceFile"]
            ?? throw new InvalidOperationException("ExportSettings:AppDSourceFile is not configured.");

    public string AppId => "AppD";

    public async Task<IEnumerable<ExportResult>> RunAsync(DataContext ctx, CancellationToken ct)
    {
        var date = DateTime.Today.ToString("yyyyMMdd");
        var fileName = $"appD_export_{date}.csv";

        try
        {
            var sourceRows = ReadSourceFile();

            // TODO: replace with your real join/transform logic.
            // sourceRows contains the data read from the disk CSV.
            // ctx.Employees / ctx.Costcenters / ctx.Accessrights are available for cross-referencing.
            var employeeLookup = ctx.Employees.ToDictionary(e => e.Id, e => e);

            var records = sourceRows
                .Select(row =>
                {
                    employeeLookup.TryGetValue(row.EmployeeId, out var emp);
                    return new AppDOutputRecord
                    {
                        EmployeeId = row.EmployeeId,
                        FullName = emp is not null ? $"{emp.FirstName} {emp.LastName}" : "Unknown",
                        Action = row.Action,
                        // TODO: add more output fields derived from row + ctx
                    };
                })
                .ToList();

            var csv = SerializeToCsv(records);
            await _fileWriter.WriteAsync(_outputDirectory, fileName, csv, ct);

            return [new ExportResult
            {
                AppId = AppId,
                FileName = fileName,
                FileSizeBytes = Encoding.UTF8.GetByteCount(csv),
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

    private List<AppDSourceRow> ReadSourceFile()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using var reader = new StreamReader(_sourceFilePath);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<AppDSourceRow>().ToList();
    }

    private static string SerializeToCsv<T>(List<T> records)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var sw = new StringWriter();
        using var csv = new CsvWriter(sw, config);
        csv.WriteRecords(records);
        return sw.ToString();
    }
}

/// <summary>
/// Represents one row in the source CSV file read from disk.
/// TODO: replace these properties with your actual column names.
/// </summary>
public class AppDSourceRow
{
    public int EmployeeId { get; set; }
    public string Action { get; set; } = string.Empty;
    // TODO: add more source columns
}

/// <summary>
/// Represents one row in the output CSV produced by AppD.
/// TODO: add/remove fields to match what AppD's target application expects.
/// </summary>
public class AppDOutputRecord
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    // TODO: add more output fields
}
