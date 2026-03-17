using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Infrastructure.Data;
using DataExportPlatform.Infrastructure.FileWriting;
using DataExportPlatform.Infrastructure.Sources;
using DataExportPlatform.Pipeline;
using DataExportPlatform.Pipeline.Jobs;
using DataExportPlatform.Pipeline.Validators;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Run as a Windows Service
builder.Host.UseWindowsService();

// ── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("DataExportPlatform.Infrastructure")));

// ── Infrastructure ──────────────────────────────────────────────────────────
builder.Services.AddSingleton<IFileWriter, LocalFileWriter>();

// Register stubs — injected directly into PipelineOrchestrator
builder.Services.AddSingleton<EmployeeSourceStub>();
builder.Services.AddSingleton<CostcenterSourceStub>();
builder.Services.AddSingleton<AccessrightSourceStub>();

// Register as IDataSource for any consumer that iterates the collection
builder.Services.AddSingleton<IDataSource>(sp => sp.GetRequiredService<EmployeeSourceStub>());
builder.Services.AddSingleton<IDataSource>(sp => sp.GetRequiredService<CostcenterSourceStub>());
builder.Services.AddSingleton<IDataSource>(sp => sp.GetRequiredService<AccessrightSourceStub>());

// ── Export jobs ──────────────────────────────────────────────────────────────
builder.Services.AddTransient<IExportJob, AppAExportJob>();
builder.Services.AddTransient<IExportJob, AppBExportJob>();
builder.Services.AddTransient<IExportJob, AppCExportJob>();

// ── Validators ───────────────────────────────────────────────────────────────
builder.Services.AddTransient<IOverrideValidator<DataExportPlatform.Core.Models.CostcenterResponsibleOverride>, CostcenterResponsibleValidator>();

// ── Pipeline ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<DataContextCache>();
builder.Services.AddSingleton<PipelineOrchestrator>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PipelineOrchestrator>());

// ── Web ───────────────────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.Run();
