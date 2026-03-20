using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Infrastructure.ActiveDirectory;
using DataExportPlatform.Infrastructure.Data;
using DataExportPlatform.Infrastructure.FileWriting;
using DataExportPlatform.Infrastructure.Sources;
using DataExportPlatform.Pipeline;
using DataExportPlatform.Pipeline.Jobs;
using DataExportPlatform.Pipeline.Validators;
using DataExportPlatform.Web.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Run as a Windows Service
builder.Host.UseWindowsService();

// ── Problem details ──────────────────────────────────────────────────────────
builder.Services.AddProblemDetails();

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("DataExportPlatform.Infrastructure")));

// ── Infrastructure ────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IFileWriter, ArchivingFileWriter>();
builder.Services.AddScoped<IAdGroupService, AdGroupService>();

builder.Services.AddSingleton<EmployeeSourceStub>();
builder.Services.AddSingleton<CostcenterSourceStub>();
builder.Services.AddSingleton<AccessrightSourceStub>();

builder.Services.AddSingleton<IDataSource>(sp => sp.GetRequiredService<EmployeeSourceStub>());
builder.Services.AddSingleton<IDataSource>(sp => sp.GetRequiredService<CostcenterSourceStub>());
builder.Services.AddSingleton<IDataSource>(sp => sp.GetRequiredService<AccessrightSourceStub>());

// ── Export jobs ───────────────────────────────────────────────────────────────
builder.Services.AddTransient<IExportJob, AppAExportJob>();
builder.Services.AddTransient<IExportJob, AppBExportJob>();
builder.Services.AddTransient<IExportJob, AppCExportJob>();
builder.Services.AddTransient<IExportJob, AppDExportJob>();

// ── Validators ────────────────────────────────────────────────────────────────
builder.Services.AddTransient<IOverrideValidator<DataExportPlatform.Core.Models.CostcenterResponsibleOverride>, CostcenterResponsibleValidator>();

// ── Pipeline ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<DataContextCache>();
builder.Services.AddSingleton<ExportArchiveService>();
builder.Services.AddSingleton<PipelineOrchestrator>();

// ── Authorization ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IAuthorizationPolicyProvider, ArchivePolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, ArchiveJobAuthorizationHandler>();
builder.Services.AddAuthorization();

// ── Authentication — Windows Auth + AD groups ─────────────────────────────────
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddScoped<IClaimsTransformation, AdGroupClaimsTransformation>();

// ── Web ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
