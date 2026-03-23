using CsvHelper;
using CsvHelper.Configuration;
using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;
using DataExportPlatform.Infrastructure.FileWriting;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;

namespace DataExportPlatform.Pipeline.Jobs;

public class AppCExportJob(IFileWriter fileWriter, IConfiguration configuration) : IExportJob
{
    private readonly IFileWriter _fileWriter = fileWriter;
    private readonly string _outputDirectory = configuration["ExportSettings:OutputDirectory"] ?? @"C:\DataExports";

    public string AppId => "AppC";

    public async Task<IEnumerable<ExportResult>> RunAsync(DataContext ctx, CancellationToken ct)
    {
        var date = DateTime.Today.ToString("yyyyMMdd");
        var results = new List<ExportResult>();

        results.Add(await ExportEmployeesAsync(ctx, date, ct));
        results.Add(await ExportCostcentersAsync(ctx, date, ct));
        results.Add(await ExportHolidaysAsync(date, ct));

        return results;
    }

    private async Task<ExportResult> ExportEmployeesAsync(DataContext ctx, string date, CancellationToken ct)
    {
        var fileName = $"appC_employees_{date}.csv";
        try
        {
            var records = ctx.Employees.Select(e => new AppCEmployeeRecord
            {
                EmployeeId = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                DepartmentCode = e.DepartmentCode,
                IsActive = e.IsActive,
            }).ToList();

            var csv = SerializeToCsv(records);
            await _fileWriter.WriteAsync(_outputDirectory, fileName, csv, ct);

            return new ExportResult
            {
                AppId = AppId,
                FileName = fileName,
                FileSizeBytes = Encoding.UTF8.GetByteCount(csv),
                RecordCount = records.Count,
                Success = true,
            };
        }
        catch (Exception ex)
        {
            return new ExportResult { AppId = AppId, FileName = fileName, Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<ExportResult> ExportCostcentersAsync(DataContext ctx, string date, CancellationToken ct)
    {
        var fileName = $"appC_costcenters_{date}.csv";
        try
        {
            var records = ctx.Costcenters.Select(c => new AppCCostcenterRecord
            {
                CostcenterId = c.Id,
                Code = c.Code,
                Name = c.Name,
                ResponsibleEmail = c.ResponsibleEmail,
            }).ToList();

            var csv = SerializeToCsv(records);
            await _fileWriter.WriteAsync(_outputDirectory, fileName, csv, ct);

            return new ExportResult
            {
                AppId = AppId,
                FileName = fileName,
                FileSizeBytes = Encoding.UTF8.GetByteCount(csv),
                RecordCount = records.Count,
                Success = true,
            };
        }
        catch (Exception ex)
        {
            return new ExportResult { AppId = AppId, FileName = fileName, Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<ExportResult> ExportHolidaysAsync(string date, CancellationToken ct)
    {
        var fileName = $"appC_holidays_{date}.csv";
        try
        {
            var records = GetHolidaysForYear(DateTime.Today.Year);

            var csv = SerializeToCsv(records);
            await _fileWriter.WriteAsync(_outputDirectory, fileName, csv, ct);

            return new ExportResult
            {
                AppId = AppId,
                FileName = fileName,
                FileSizeBytes = Encoding.UTF8.GetByteCount(csv),
                RecordCount = records.Count,
                Success = true,
            };
        }
        catch (Exception ex)
        {
            return new ExportResult { AppId = AppId, FileName = fileName, Success = false, ErrorMessage = ex.Message };
        }
    }

    private static List<AppCHolidayRecord> GetHolidaysForYear(int year)
    {
        // Easter-based calculations
        var easter = ComputeEaster(year);

        string Fmt(DateTime d) => d.ToString("yyyy-MM-dd");

        return
        [
            new() { Date = Fmt(new DateTime(year, 1, 1)),  HolidayName = "New Year's Day",       Type = "Public" },
            new() { Date = Fmt(easter.AddDays(-2)),         HolidayName = "Good Friday",           Type = "Public" },
            new() { Date = Fmt(easter),                     HolidayName = "Easter Sunday",         Type = "Public" },
            new() { Date = Fmt(easter.AddDays(1)),          HolidayName = "Easter Monday",         Type = "Public" },
            new() { Date = Fmt(easter.AddDays(39)),         HolidayName = "Ascension Day",         Type = "Public" },
            new() { Date = Fmt(easter.AddDays(49)),         HolidayName = "Whit Sunday",           Type = "Public" },
            new() { Date = Fmt(easter.AddDays(50)),         HolidayName = "Whit Monday",           Type = "Public" },
            new() { Date = Fmt(new DateTime(year, 4, 27)),  HolidayName = "King's Day",            Type = "Public" },
            new() { Date = Fmt(new DateTime(year, 5, 5)),   HolidayName = "Liberation Day",        Type = "Public" },
            new() { Date = Fmt(new DateTime(year, 12, 25)), HolidayName = "Christmas Day",         Type = "Public" },
            new() { Date = Fmt(new DateTime(year, 12, 26)), HolidayName = "Second Christmas Day",  Type = "Public" },
        ];
    }

    /// <summary>Anonymous Gregorian algorithm for computing Easter Sunday.</summary>
    private static DateTime ComputeEaster(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day   = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateTime(year, month, day);
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

public class AppCEmployeeRecord
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AppCCostcenterRecord
{
    public int CostcenterId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ResponsibleEmail { get; set; } = string.Empty;
}

public class AppCHolidayRecord
{
    public string Date { get; set; } = string.Empty;
    public string HolidayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
