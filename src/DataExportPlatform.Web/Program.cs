using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Infrastructure.Data;
using DataExportPlatform.Infrastructure.FileWriting;
using DataExportPlatform.Infrastructure.Sources;
using DataExportPlatform.Pipeline;
using DataExportPlatform.Pipeline.Jobs;
using DataExportPlatform.Pipeline.Validators;
using DataExportPlatform.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
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
builder.Services.AddSingleton<IFileWriter, ArchivingFileWriter>();

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
builder.Services.AddTransient<IExportJob, AppDExportJob>();

// ── Validators ───────────────────────────────────────────────────────────────
builder.Services.AddTransient<IOverrideValidator<DataExportPlatform.Core.Models.CostcenterResponsibleOverride>, CostcenterResponsibleValidator>();

// ── Pipeline ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<DataContextCache>();
builder.Services.AddSingleton<ExportArchiveService>();
builder.Services.AddSingleton<PipelineOrchestrator>();

// ── Authorization ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IAuthorizationPolicyProvider, ArchivePolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, ArchiveJobAuthorizationHandler>();
builder.Services.AddAuthorization();

// ── Authentication — Windows Auth + AD groups ─────────────────────────────────
// Uncomment the block below and configure Authorization:ArchiveJobGroups in
// appsettings.json to enable per-job access control via AD group membership.
//
// Also enable Windows Authentication in Properties/launchSettings.json:
//   "windowsAuthentication": true, "anonymousAuthentication": false
// And in web.config (when deployed as a Windows Service behind IIS):
//   <windowsAuthentication enabled="true" />
//
// builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
//     .AddNegotiate();
//
// builder.Services.AddScoped<IClaimsTransformation, AdGroupClaimsTransformation>();
//
// app.UseAuthentication();   // place this before app.UseAuthorization()

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
