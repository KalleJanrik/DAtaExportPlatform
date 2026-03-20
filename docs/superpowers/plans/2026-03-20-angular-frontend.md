# Angular 21 Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Razor Pages UI with an Angular 21 SPA served from the existing ASP.NET Core host, with Windows Authentication and a Handelsbanken-themed Angular Material design.

**Architecture:** Angular 21 standalone components built to `wwwroot/` via `ng build`; ASP.NET Core serves static files and falls back to `index.html` for all non-API routes. Four new/modified API controllers expose JSON endpoints consumed by Angular services.

**Tech Stack:** Angular 21, Angular Material 21, RxJS, TypeScript, ASP.NET Core 8, C# records (DTOs), EF Core 8, SQL Server.

---

## File Map

### Backend — New / Modified

| Path | Action | Responsibility |
|------|--------|----------------|
| `src/DataExportPlatform.Web/Program.cs` | Modify | Enable Windows Auth, ProblemDetails, JsonStringEnumConverter, static files, SPA fallback |
| `src/DataExportPlatform.Web/Properties/launchSettings.json` | Modify | Enable Windows Auth for dev |
| `.gitignore` | Modify | Ignore wwwroot/, ClientApp/node_modules/, .superpowers/ |
| `src/DataExportPlatform.Web/Controllers/PipelineController.cs` | Modify | Add `[Authorize]`, remove try/catch |
| `src/DataExportPlatform.Web/Controllers/AuthController.cs` | Create | `GET /api/auth/whoami` |
| `src/DataExportPlatform.Web/Controllers/Dtos.cs` | Create | All shared DTO records |
| `src/DataExportPlatform.Web/Controllers/RunsController.cs` | Create | `GET /api/runs`, `GET /api/runs/{id}` |
| `src/DataExportPlatform.Web/Controllers/ArchiveController.cs` | Create | `GET /api/archive`, `GET /api/archive/{appId}`, `GET /api/archive/{appId}/{day}/{file}` |
| `src/DataExportPlatform.Web/Pages/` | Delete | Entire directory removed |

### Angular — New Files

| Path | Responsibility |
|------|----------------|
| `src/DataExportPlatform.Web/ClientApp/` | Angular 21 project root |
| `src/DataExportPlatform.Web/ClientApp/proxy.conf.json` | Dev proxy: /api → localhost:49467 |
| `src/DataExportPlatform.Web/ClientApp/src/styles.scss` | Handelsbanken Material theme |
| `src/DataExportPlatform.Web/ClientApp/src/app/app.config.ts` | Root providers: router, HttpClient, interceptors |
| `src/DataExportPlatform.Web/ClientApp/src/app/app.routes.ts` | Route definitions |
| `src/DataExportPlatform.Web/ClientApp/src/app/app.component.ts/.html/.scss` | Shell: MatSidenav + MatToolbar |
| `src/DataExportPlatform.Web/ClientApp/src/app/interceptors/credentials.interceptor.ts` | Adds `withCredentials: true` to all requests |
| `src/DataExportPlatform.Web/ClientApp/src/app/interceptors/http-error.interceptor.ts` | Global snackbar error handling |
| `src/DataExportPlatform.Web/ClientApp/src/app/models/api.models.ts` | TypeScript interfaces matching backend DTOs |
| `src/DataExportPlatform.Web/ClientApp/src/app/services/auth.service.ts` | GET /api/auth/whoami |
| `src/DataExportPlatform.Web/ClientApp/src/app/services/runs.service.ts` | GET /api/runs, /api/runs/:id |
| `src/DataExportPlatform.Web/ClientApp/src/app/services/pipeline.service.ts` | POST /api/pipeline/trigger |
| `src/DataExportPlatform.Web/ClientApp/src/app/services/archive.service.ts` | GET /api/archive, /api/archive/:appId |
| `src/DataExportPlatform.Web/ClientApp/src/app/services/polling.service.ts` | Polling logic with BehaviorSubject |
| `src/DataExportPlatform.Web/ClientApp/src/app/dashboard/dashboard.component.ts/.html/.scss` | KPI cards + runs table |
| `src/DataExportPlatform.Web/ClientApp/src/app/run-detail/run-detail.component.ts/.html/.scss` | Single run + export log table |
| `src/DataExportPlatform.Web/ClientApp/src/app/trigger/trigger.component.ts/.html/.scss` | Trigger button + snackbar feedback |
| `src/DataExportPlatform.Web/ClientApp/src/app/archive-index/archive-index.component.ts/.html/.scss` | Job summary cards |
| `src/DataExportPlatform.Web/ClientApp/src/app/archive-job/archive-job.component.ts/.html/.scss` | Day accordion + file download links |

---

## Task 1: Update .gitignore and Backend Foundation

**Files:**
- Modify: `.gitignore`
- Modify: `src/DataExportPlatform.Web/Properties/launchSettings.json`
- Modify: `src/DataExportPlatform.Web/Program.cs`

- [ ] **Step 1: Add entries to .gitignore**

Open `.gitignore` at the repo root and append:

```
src/DataExportPlatform.Web/wwwroot/
src/DataExportPlatform.Web/ClientApp/node_modules/
src/DataExportPlatform.Web/ClientApp/dist/
.superpowers/
```

- [ ] **Step 2: Enable Windows Auth in launchSettings.json**

Replace the entire content of `src/DataExportPlatform.Web/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "DataExportPlatform.Web": {
      "commandName": "Project",
      "launchBrowser": true,
      "windowsAuthentication": true,
      "anonymousAuthentication": false,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "https://localhost:49466;http://localhost:49467"
    }
  }
}
```

- [ ] **Step 3: Rewrite Program.cs**

Replace the entire content of `src/DataExportPlatform.Web/Program.cs`:

```csharp
using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Infrastructure.ActiveDirectory;
using DataExportPlatform.Infrastructure.Data;
using DataExportPlatform.Infrastructure.FileWriting;
using DataExportPlatform.Infrastructure.Sources;
using DataExportPlatform.Pipeline;
using DataExportPlatform.Pipeline.Jobs;
using DataExportPlatform.Pipeline.Validators;
using DataExportPlatform.Web.Authorization;
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
```

- [ ] **Step 4: Verify the backend compiles**

```bash
dotnet build src/DataExportPlatform.Web
```

Expected: `Build succeeded` with 0 errors. (Warnings about missing `Microsoft.AspNetCore.Authentication.Negotiate` namespace are OK if the NuGet package needs adding — see next step.)

- [ ] **Step 5: Add Negotiate NuGet package if build fails**

If step 4 errors on `NegotiateDefaults` or `AddNegotiate()`:

```bash
dotnet add src/DataExportPlatform.Web package Microsoft.AspNetCore.Authentication.Negotiate
dotnet build src/DataExportPlatform.Web
```

Expected: `Build succeeded`.

- [ ] **Step 6: Commit**

```bash
git add .gitignore src/DataExportPlatform.Web/Properties/launchSettings.json src/DataExportPlatform.Web/Program.cs src/DataExportPlatform.Web/DataExportPlatform.Web.csproj
git commit -m "feat: enable Windows Auth, ProblemDetails, enum serialization, SPA fallback"
```

---

## Task 2: Update PipelineController

**Files:**
- Modify: `src/DataExportPlatform.Web/Controllers/PipelineController.cs`

- [ ] **Step 1: Replace PipelineController content**

```csharp
using DataExportPlatform.Pipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/pipeline")]
public class PipelineController : ControllerBase
{
    private readonly PipelineOrchestrator _orchestrator;
    private readonly ILogger<PipelineController> _logger;

    public PipelineController(PipelineOrchestrator orchestrator, ILogger<PipelineController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Runs the pipeline synchronously. Returns 200 on success; exceptions bubble to UseExceptionHandler().
    /// Designed to be called by an external scheduler (Task Scheduler, cron, Azure Logic Apps, etc.).
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        _logger.LogInformation("Pipeline trigger requested.");
        await _orchestrator.RunAsync(ct);
        return Ok(new { message = "Pipeline run completed successfully." });
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build src/DataExportPlatform.Web
```

Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add src/DataExportPlatform.Web/Controllers/PipelineController.cs
git commit -m "feat: add [Authorize] to PipelineController, remove try/catch"
```

---

## Task 3: Create DTOs and AuthController

**Files:**
- Create: `src/DataExportPlatform.Web/Controllers/Dtos.cs`
- Create: `src/DataExportPlatform.Web/Controllers/AuthController.cs`

- [ ] **Step 1: Create Dtos.cs**

```csharp
namespace DataExportPlatform.Web.Controllers;

// ── Runs ──────────────────────────────────────────────────────────────────────

public record PipelineRunSummaryDto(
    int Id,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string Status,
    string? ErrorMessage,
    IEnumerable<ExportLogDto> ExportLogs);

public record ExportLogDto(
    string AppId,
    string FileName,
    int RecordCount,
    long FileSizeBytes,
    string Status,
    DateTime ExportedAt,
    string? ErrorMessage);

// ── Archive ───────────────────────────────────────────────────────────────────

public record ArchiveSummaryDto(string AppId, int DayCount, int FileCount, string? LatestDay);

public record ArchiveJobDto(string AppId, IEnumerable<DayGroupDto> Days);

public record DayGroupDto(string Day, IEnumerable<ArchivedFileDto> Files);

public record ArchivedFileDto(string FileName, long SizeBytes);
```

- [ ] **Step 2: Create AuthController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("whoami")]
    public IActionResult WhoAmI() =>
        Ok(new { username = User.Identity?.Name ?? string.Empty });
}
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build src/DataExportPlatform.Web
```

Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add src/DataExportPlatform.Web/Controllers/Dtos.cs src/DataExportPlatform.Web/Controllers/AuthController.cs
git commit -m "feat: add DTOs and AuthController (GET /api/auth/whoami)"
```

---

## Task 4: Create RunsController

**Files:**
- Create: `src/DataExportPlatform.Web/Controllers/RunsController.cs`

- [ ] **Step 1: Create RunsController.cs**

```csharp
using DataExportPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/runs")]
public class RunsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RunsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetRuns()
    {
        var runs = await _db.PipelineRuns
            .Include(r => r.ExportLogs)
            .OrderByDescending(r => r.StartedAt)
            .Take(10)
            .ToListAsync();

        var dtos = runs.Select(r => new PipelineRunSummaryDto(
            r.Id,
            r.StartedAt,
            r.FinishedAt,
            r.Status.ToString(),
            r.ErrorMessage,
            r.ExportLogs.Select(l => new ExportLogDto(
                l.AppId, l.FileName, l.RecordCount, l.FileSizeBytes,
                l.Status.ToString(), l.ExportedAt, l.ErrorMessage))));

        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRun(int id)
    {
        var run = await _db.PipelineRuns
            .Include(r => r.ExportLogs)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (run is null)
            return NotFound();

        var dto = new PipelineRunSummaryDto(
            run.Id,
            run.StartedAt,
            run.FinishedAt,
            run.Status.ToString(),
            run.ErrorMessage,
            run.ExportLogs.Select(l => new ExportLogDto(
                l.AppId, l.FileName, l.RecordCount, l.FileSizeBytes,
                l.Status.ToString(), l.ExportedAt, l.ErrorMessage)));

        return Ok(dto);
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build src/DataExportPlatform.Web
```

Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add src/DataExportPlatform.Web/Controllers/RunsController.cs
git commit -m "feat: add RunsController (GET /api/runs, GET /api/runs/{id})"
```

---

## Task 5: Create ArchiveController

**Files:**
- Create: `src/DataExportPlatform.Web/Controllers/ArchiveController.cs`

- [ ] **Step 1: Create ArchiveController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataExportPlatform.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/archive")]
public class ArchiveController : ControllerBase
{
    private static readonly string[] KnownAppIds = ["AppA", "AppB", "AppC", "AppD"];

    private readonly string _archiveRoot;
    private readonly IAuthorizationService _authorizationService;

    public ArchiveController(IConfiguration configuration, IAuthorizationService authorizationService)
    {
        _archiveRoot = configuration["ExportSettings:ArchiveRoot"] ?? @"C:\DataExports\Archive";
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public IActionResult GetSummaries()
    {
        var summaries = KnownAppIds
            .Select(appId =>
            {
                var jobDir = Path.Combine(_archiveRoot, appId);
                if (!Directory.Exists(jobDir))
                    return null;

                var dayDirs = Directory.EnumerateDirectories(jobDir).ToList();
                var fileCount = dayDirs.Sum(d => Directory.EnumerateFiles(d).Count());
                var latestDay = dayDirs
                    .Select(d => Path.GetFileName(d))
                    .Where(n => DateTime.TryParse(n, out _))
                    .OrderDescending()
                    .FirstOrDefault();

                return new ArchiveSummaryDto(appId, dayDirs.Count, fileCount, latestDay);
            })
            .Where(s => s is not null)
            .ToList();

        return Ok(summaries);
    }

    [HttpGet("{appId}")]
    public async Task<IActionResult> GetJob(string appId)
    {
        if (!IsKnownAppId(appId))
            return NotFound();

        var auth = await _authorizationService.AuthorizeAsync(User, null, $"Archive.{appId}");
        if (!auth.Succeeded)
            return Forbid();

        var jobDir = Path.Combine(_archiveRoot, appId);
        if (!Directory.Exists(jobDir))
            return NotFound();

        var days = Directory
            .EnumerateDirectories(jobDir)
            .Where(d => DateTime.TryParse(Path.GetFileName(d), out _))
            .OrderByDescending(d => Path.GetFileName(d))
            .Select(d => new DayGroupDto(
                Path.GetFileName(d),
                Directory.EnumerateFiles(d)
                    .Select(f => new ArchivedFileDto(Path.GetFileName(f), new FileInfo(f).Length))
                    .OrderBy(f => f.FileName)));

        return Ok(new ArchiveJobDto(appId, days));
    }

    [HttpGet("{appId}/{day}/{file}")]
    public async Task<IActionResult> Download(string appId, string day, string file)
    {
        if (!IsKnownAppId(appId))
            return NotFound();

        // Path traversal guard
        if (ContainsPathSeparator(appId) || ContainsPathSeparator(day) || ContainsPathSeparator(file))
            return BadRequest();

        var auth = await _authorizationService.AuthorizeAsync(User, null, $"Archive.{appId}");
        if (!auth.Succeeded)
            return Forbid();

        var path = Path.Combine(_archiveRoot, appId, day, file);
        if (!System.IO.File.Exists(path))
            return NotFound();

        var stream = System.IO.File.OpenRead(path);
        var contentType = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? "application/json"
            : path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? "text/csv"
            : "application/octet-stream";

        return File(stream, contentType, file);
    }

    private static bool IsKnownAppId(string appId) =>
        KnownAppIds.Contains(appId, StringComparer.OrdinalIgnoreCase);

    private static bool ContainsPathSeparator(string s) =>
        s.Contains('/') || s.Contains('\\');
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build src/DataExportPlatform.Web
```

Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add src/DataExportPlatform.Web/Controllers/ArchiveController.cs
git commit -m "feat: add ArchiveController (GET /api/archive, /{appId}, /{appId}/{day}/{file})"
```

---

## Task 6: Delete Pages Directory and Run Tests

**Files:**
- Delete: `src/DataExportPlatform.Web/Pages/` (entire directory)

- [ ] **Step 1: Delete the Pages directory**

```bash
rm -rf src/DataExportPlatform.Web/Pages
```

- [ ] **Step 2: Build to verify nothing broke**

```bash
dotnet build
```

Expected: `Build succeeded` with 0 errors. (The solution has no references to the deleted pages.)

- [ ] **Step 3: Run existing tests**

```bash
dotnet test
```

Expected: All existing tests pass. No new tests are added in this feature (Angular tests are out of scope; backend controllers have no dedicated tests per spec scope).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: remove Razor Pages directory — backend API-only"
```

---

## Task 7: Scaffold Angular Project

**Files:**
- Create: `src/DataExportPlatform.Web/ClientApp/` (entire Angular project)

Prerequisites: Node.js 20+ and Angular CLI 21 installed globally (`npm install -g @angular/cli@21`).

- [ ] **Step 1: Clear wwwroot if it has existing content**

```bash
rm -rf src/DataExportPlatform.Web/wwwroot/*
```

- [ ] **Step 2: Scaffold Angular project**

```bash
cd src/DataExportPlatform.Web
ng new ClientApp --routing --style=scss --skip-git
```

When prompted for SSR: choose **No**. Accept all other defaults.

- [ ] **Step 3: Add Angular Material**

```bash
cd ClientApp
ng add @angular/material
```

When prompted:
- Choose theme: **Custom**
- Set up global typography styles: **Yes**
- Include animations: **Yes (enabled)**

- [ ] **Step 4: Verify Angular project builds**

```bash
ng build
```

Expected: Build succeeds and output appears in `src/DataExportPlatform.Web/wwwroot/`.

- [ ] **Step 5: Update angular.json outputPath**

In `ClientApp/angular.json`, find the `"outputPath"` key under `projects > ClientApp > architect > build > options`. Change it to:

```json
"outputPath": "../wwwroot"
```

Also change `"browser"` sub-key if present (Angular 17+ splits into `"browser"` and optionally `"server"`):

```json
"outputPath": {
  "base": "../wwwroot",
  "browser": ""
}
```

> Note: Angular 17+ sets `outputPath` as an object with `base` and `browser` keys. If `ng build` produced files in `wwwroot/browser/`, use the object form above so files land directly in `wwwroot/`. Verify by checking where `index.html` ends up after `ng build`.

- [ ] **Step 6: Re-run ng build to confirm output path**

```bash
ng build
```

Expected: `wwwroot/index.html` exists (not `wwwroot/browser/index.html`).

- [ ] **Step 7: Commit**

```bash
cd ../..
git add src/DataExportPlatform.Web/ClientApp/
git commit -m "feat: scaffold Angular 21 project with Angular Material"
```

---

## Task 8: Theme, Global Styles, and Models

**Files:**
- Modify: `src/DataExportPlatform.Web/ClientApp/src/styles.scss`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/models/api.models.ts`

- [ ] **Step 1: Replace styles.scss with Handelsbanken theme**

```scss
@use '@angular/material' as mat;

$dep-theme: mat.define-theme((
  color: (
    theme-type: light,
    primary: mat.$azure-palette,
    tertiary: mat.$red-palette,
  ),
));

html {
  @include mat.all-component-themes($dep-theme);
}

:root {
  --mat-sys-primary: #00205B;
  --mat-sys-on-primary: #ffffff;
  --mat-sys-tertiary: #DA1F2B;
  --mat-sys-on-tertiary: #ffffff;
}

body {
  margin: 0;
  font-family: Roboto, "Helvetica Neue", sans-serif;
  background: #f5f5f5;
}

.sidenav-container {
  height: 100vh;
}

.sidenav {
  width: 220px;
  background: #00205B;
  color: white;
}

.sidenav .mat-mdc-list-item {
  color: white;
}

.sidenav .nav-item-active {
  border-left: 3px solid #DA1F2B;
  background: rgba(255,255,255,0.08);
}

.mat-toolbar {
  background: #00205B;
  color: white;
}

.content-area {
  padding: 24px;
  background: white;
  min-height: calc(100vh - 64px);
}

.live-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: #4caf50;
  margin-left: 12px;
}

.live-badge::before {
  content: '●';
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}

.status-badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
}
.status-success { background: #e8f5e9; color: #2e7d32; }
.status-failed { background: #ffebee; color: #c62828; }
.status-partialfailure { background: #fff3e0; color: #e65100; }
.status-running { background: #e3f2fd; color: #1565c0; }

.kpi-card {
  border-top: 3px solid #00205B;
  padding: 16px;
}
.kpi-card.kpi-error { border-top-color: #DA1F2B; }

.error-message {
  color: #c62828;
  padding: 16px;
}

.access-denied {
  color: #e65100;
  padding: 16px;
}
```

- [ ] **Step 2: Create api.models.ts**

```typescript
// src/app/models/api.models.ts

export interface ExportLogDto {
  appId: string;
  fileName: string;
  recordCount: number;
  fileSizeBytes: number;
  status: 'Success' | 'Failed';
  exportedAt: string;
  errorMessage?: string;
}

export interface PipelineRunDto {
  id: number;
  startedAt: string;
  finishedAt?: string;
  status: 'Running' | 'Success' | 'PartialFailure' | 'Failed';
  errorMessage?: string;
  exportLogs: ExportLogDto[];
}

export interface ArchiveSummaryDto {
  appId: string;
  dayCount: number;
  fileCount: number;
  latestDay?: string;
}

export interface ArchivedFileDto {
  fileName: string;
  sizeBytes: number;
}

export interface DayGroupDto {
  day: string;
  files: ArchivedFileDto[];
}

export interface ArchiveJobDto {
  appId: string;
  days: DayGroupDto[];
}
```

- [ ] **Step 3: Build to verify**

```bash
cd src/DataExportPlatform.Web/ClientApp
ng build
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
cd ../../..
git add src/DataExportPlatform.Web/ClientApp/src/styles.scss src/DataExportPlatform.Web/ClientApp/src/app/models/
git commit -m "feat: add Handelsbanken theme and API model interfaces"
```

---

## Task 9: Interceptors and App Config

**Files:**
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/interceptors/credentials.interceptor.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/interceptors/http-error.interceptor.ts`
- Modify: `src/DataExportPlatform.Web/ClientApp/src/app/app.config.ts`
- Modify: `src/DataExportPlatform.Web/ClientApp/src/app/app.routes.ts`

- [ ] **Step 1: Create credentials interceptor**

```typescript
// src/app/interceptors/credentials.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';

export const credentialsInterceptor: HttpInterceptorFn = (req, next) =>
  next(req.clone({ withCredentials: true }));
```

- [ ] **Step 2: Create http-error interceptor**

```typescript
// src/app/interceptors/http-error.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  // Polling requests are tagged — swallow all errors silently
  if (req.headers.has('X-Silent-Error')) {
    return next(req).pipe(catchError(() => throwError(() => null)));
  }

  return next(req).pipe(
    catchError(err => {
      const status = err?.status;

      if (status === 401) {
        snackBar.open('You are not authorized.', 'Close', { duration: 5000 });
      } else if (status >= 500) {
        const detail = err?.error?.detail ?? err?.message ?? 'An error occurred.';
        snackBar.open(detail, 'Close', { duration: 5000 });
      }
      // 403 and 404: re-throw only — component handles inline

      return throwError(() => err);
    })
  );
};
```

- [ ] **Step 3: Create app.routes.ts**

```typescript
// src/app/app.routes.ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'runs/:id', loadComponent: () => import('./run-detail/run-detail.component').then(m => m.RunDetailComponent) },
  { path: 'trigger', loadComponent: () => import('./trigger/trigger.component').then(m => m.TriggerComponent) },
  { path: 'archive', loadComponent: () => import('./archive-index/archive-index.component').then(m => m.ArchiveIndexComponent) },
  { path: 'archive/:appId', loadComponent: () => import('./archive-job/archive-job.component').then(m => m.ArchiveJobComponent) },
  { path: '**', redirectTo: '' },
];
```

- [ ] **Step 4: Rewrite app.config.ts**

```typescript
// src/app/app.config.ts
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { routes } from './app.routes';
import { credentialsInterceptor } from './interceptors/credentials.interceptor';
import { httpErrorInterceptor } from './interceptors/http-error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([credentialsInterceptor, httpErrorInterceptor])),
    provideAnimations(),
  ],
};
```

- [ ] **Step 5: Build to verify**

```bash
ng build
```

Expected: Build succeeds. (Route components don't exist yet but lazy imports only fail at runtime.)

- [ ] **Step 6: Commit**

```bash
cd ../../..
git add src/DataExportPlatform.Web/ClientApp/src/app/interceptors/ src/DataExportPlatform.Web/ClientApp/src/app/app.config.ts src/DataExportPlatform.Web/ClientApp/src/app/app.routes.ts
git commit -m "feat: add credentials/error interceptors and app routing config"
```

---

## Task 10: Services

**Files:**
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/services/auth.service.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/services/runs.service.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/services/pipeline.service.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/services/archive.service.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/services/polling.service.ts`

- [ ] **Step 1: Create auth.service.ts**

```typescript
// src/app/services/auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly username$ = this.http
    .get<{ username: string }>('/api/auth/whoami')
    .pipe(map(r => r.username), shareReplay(1));

  constructor(private http: HttpClient) {}

  getUsername(): Observable<string> {
    return this.username$;
  }
}
```

- [ ] **Step 2: Create runs.service.ts**

```typescript
// src/app/services/runs.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PipelineRunDto } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class RunsService {
  constructor(private http: HttpClient) {}

  getRuns(): Observable<PipelineRunDto[]> {
    return this.http.get<PipelineRunDto[]>('/api/runs');
  }

  getRun(id: number): Observable<PipelineRunDto> {
    return this.http.get<PipelineRunDto>(`/api/runs/${id}`);
  }
}
```

- [ ] **Step 3: Create pipeline.service.ts**

```typescript
// src/app/services/pipeline.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PipelineService {
  constructor(private http: HttpClient) {}

  trigger(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/pipeline/trigger', {});
  }
}
```

- [ ] **Step 4: Create archive.service.ts**

```typescript
// src/app/services/archive.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ArchiveSummaryDto, ArchiveJobDto } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ArchiveService {
  constructor(private http: HttpClient) {}

  getSummaries(): Observable<ArchiveSummaryDto[]> {
    return this.http.get<ArchiveSummaryDto[]>('/api/archive');
  }

  getJob(appId: string): Observable<ArchiveJobDto> {
    return this.http.get<ArchiveJobDto>(`/api/archive/${appId}`);
  }

  buildDownloadUrl(appId: string, day: string, fileName: string): string {
    return `/api/archive/${encodeURIComponent(appId)}/${encodeURIComponent(day)}/${encodeURIComponent(fileName)}`;
  }
}
```

- [ ] **Step 5: Create polling.service.ts**

```typescript
// src/app/services/polling.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, interval, Observable, of } from 'rxjs';
import { switchMap, takeWhile, catchError } from 'rxjs/operators';
import { PipelineRunDto } from '../models/api.models';

const SILENT_HEADERS = new HttpHeaders({ 'X-Silent-Error': 'true' });

@Injectable({ providedIn: 'root' })
export class PollingService {
  readonly isPolling$ = new BehaviorSubject<boolean>(false);

  constructor(private http: HttpClient) {}

  /**
   * Returns an Observable that:
   * 1. Immediately fetches /api/runs (using normal error handling — NOT silent).
   * 2. If any run is Running, starts polling every 5 seconds (silent errors).
   * 3. Stops when no run is Running.
   * Subscribe in ngOnInit, unsubscribe in ngOnDestroy.
   */
  getRuns$(): Observable<PipelineRunDto[]> {
    return new Observable<PipelineRunDto[]>(subscriber => {
      this.isPolling$.next(false);

      // Initial fetch — normal error handling (NOT silent)
      this.http.get<PipelineRunDto[]>('/api/runs').subscribe({
        next: runs => {
          subscriber.next(runs);
          const hasRunning = runs.some(r => r.status === 'Running');
          if (!hasRunning) {
            subscriber.complete();
            return;
          }

          this.isPolling$.next(true);

          interval(5000)
            .pipe(
              switchMap(() =>
                this.http.get<PipelineRunDto[]>('/api/runs', { headers: SILENT_HEADERS }).pipe(
                  catchError(() => of([] as PipelineRunDto[]))
                )
              ),
              takeWhile(runs => {
                subscriber.next(runs);
                return runs.some(r => r.status === 'Running');
              }, true)
            )
            .subscribe({
              complete: () => {
                this.isPolling$.next(false);
                subscriber.complete();
              },
            });
        },
        error: err => subscriber.error(err),
      });
    });
  }
}
```

- [ ] **Step 6: Build to verify**

```bash
ng build
```

Expected: Build succeeds.

- [ ] **Step 7: Commit**

```bash
cd ../../..
git add src/DataExportPlatform.Web/ClientApp/src/app/services/
git commit -m "feat: add Angular services (auth, runs, pipeline, archive, polling)"
```

---

## Task 11: App Shell (AppComponent)

**Files:**
- Modify: `src/DataExportPlatform.Web/ClientApp/src/app/app.component.ts`
- Modify: `src/DataExportPlatform.Web/ClientApp/src/app/app.component.html`
- Modify: `src/DataExportPlatform.Web/ClientApp/src/app/app.component.scss`

- [ ] **Step 1: Rewrite app.component.ts**

```typescript
// src/app/app.component.ts
import { Component, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from './services/auth.service';
import { PollingService } from './services/polling.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe,
    MatSidenavModule, MatToolbarModule, MatListModule, MatDividerModule,
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  username$!: Observable<string>;
  isPolling$!: Observable<boolean>;

  constructor(
    private auth: AuthService,
    public pollingService: PollingService,
  ) {}

  ngOnInit() {
    this.username$ = this.auth.getUsername();
    this.isPolling$ = this.pollingService.isPolling$;
  }
}
```

- [ ] **Step 2: Rewrite app.component.html**

```html
<!-- src/app/app.component.html -->
<mat-sidenav-container class="sidenav-container">

  <mat-sidenav class="sidenav" mode="side" opened>
    <div style="padding: 20px 16px 12px; border-bottom: 1px solid rgba(255,255,255,0.15);">
      <div style="font-size: 15px; font-weight: 700; letter-spacing: 0.5px;">DataExport Platform</div>
      <div style="font-size: 11px; opacity: 0.55; margin-top: 4px;">{{ username$ | async }}</div>
    </div>

    <mat-nav-list>
      <a mat-list-item routerLink="/" routerLinkActive="nav-item-active" [routerLinkActiveOptions]="{exact:true}">
        📊 Dashboard
      </a>
      <a mat-list-item routerLink="/archive" routerLinkActive="nav-item-active">
        🗄 Archive
      </a>
      <mat-divider style="border-color: rgba(255,255,255,0.1); margin: 8px 0;"></mat-divider>
      <a mat-list-item routerLink="/trigger" routerLinkActive="nav-item-active">
        ▶ Trigger Run
      </a>
    </mat-nav-list>
  </mat-sidenav>

  <mat-sidenav-content>
    <mat-toolbar>
      <span>DataExport</span>
      @if (isPolling$ | async) {<span class="live-badge">Live</span>}
    </mat-toolbar>

    <div class="content-area">
      <router-outlet></router-outlet>
    </div>
  </mat-sidenav-content>

</mat-sidenav-container>
```

- [ ] **Step 3: Empty app.component.scss** (styles are in global styles.scss)

```scss
// app-level styles are in styles.scss
```

- [ ] **Step 4: Build to verify**

```bash
ng build
```

Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
cd ../../..
git add src/DataExportPlatform.Web/ClientApp/src/app/app.component.ts src/DataExportPlatform.Web/ClientApp/src/app/app.component.html src/DataExportPlatform.Web/ClientApp/src/app/app.component.scss
git commit -m "feat: add app shell with Handelsbanken sidenav and toolbar"
```

---

## Task 12: DashboardComponent

**Files:**
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/dashboard/dashboard.component.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/dashboard/dashboard.component.html`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/dashboard/dashboard.component.scss`

- [ ] **Step 1: Create dashboard.component.ts**

```typescript
// src/app/dashboard/dashboard.component.ts
import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { Subscription } from 'rxjs';
import { PipelineRunDto } from '../models/api.models';
import { PollingService } from '../services/polling.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule, MatButtonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit, OnDestroy {
  runs: PipelineRunDto[] = [];
  displayedColumns = ['startedAt', 'duration', 'status', 'jobs', 'actions'];
  private sub?: Subscription;

  constructor(private polling: PollingService, private router: Router) {}

  ngOnInit() {
    this.sub = this.polling.getRuns$().subscribe({
      next: runs => (this.runs = runs),
    });
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }

  duration(run: PipelineRunDto): string {
    if (!run.finishedAt) return '—';
    const ms = new Date(run.finishedAt).getTime() - new Date(run.startedAt).getTime();
    const s = Math.floor(ms / 1000);
    return s < 60 ? `${s}s` : `${Math.floor(s / 60)}m ${s % 60}s`;
  }

  jobs(run: PipelineRunDto): string {
    return [...new Set(run.exportLogs.map(l => l.appId))].join(' ');
  }

  viewRun(id: number) {
    this.router.navigate(['/runs', id]);
  }

  get lastRun(): PipelineRunDto | undefined {
    return this.runs[0];
  }

  get lastFailed(): PipelineRunDto | undefined {
    return this.runs.find(r => r.status === 'Failed' || r.status === 'PartialFailure');
  }

  statusClass(status: string): string {
    return 'status-' + status.toLowerCase();
  }
}
```

- [ ] **Step 2: Create dashboard.component.html**

```html
<!-- src/app/dashboard/dashboard.component.html -->
<h2 style="color: #00205B; margin-top: 0;">Dashboard</h2>

<!-- KPI Cards -->
<div style="display: flex; gap: 16px; margin-bottom: 24px; flex-wrap: wrap;">
  <mat-card class="kpi-card" style="flex: 1; min-width: 160px;">
    <mat-card-content>
      <div style="font-size:11px; color:#888; text-transform:uppercase; margin-bottom:4px;">Last Run</div>
      <div *ngIf="lastRun; else noRun">
        <span [class]="'status-badge ' + statusClass(lastRun.status)">{{ lastRun.status }}</span>
        <div style="font-size:11px; color:#aaa; margin-top:4px;">{{ lastRun.startedAt | date:'short' }}</div>
      </div>
      <ng-template #noRun><span style="color:#aaa;">No runs yet</span></ng-template>
    </mat-card-content>
  </mat-card>

  <mat-card class="kpi-card kpi-error" style="flex: 1; min-width: 160px;">
    <mat-card-content>
      <div style="font-size:11px; color:#888; text-transform:uppercase; margin-bottom:4px;">Last Failed</div>
      <div *ngIf="lastFailed; else noFail">
        <span class="status-badge status-failed">{{ lastFailed.status }}</span>
        <div style="font-size:11px; color:#aaa; margin-top:4px;">{{ lastFailed.startedAt | date:'short' }}</div>
      </div>
      <ng-template #noFail><span style="color:#aaa;">None</span></ng-template>
    </mat-card-content>
  </mat-card>

  <mat-card class="kpi-card" style="flex: 1; min-width: 160px;">
    <mat-card-content>
      <div style="font-size:11px; color:#888; text-transform:uppercase; margin-bottom:4px;">Total Runs</div>
      <div style="font-size:20px; font-weight:600; color:#00205B;">{{ runs.length }}</div>
    </mat-card-content>
  </mat-card>
</div>

<!-- Runs Table -->
<mat-card>
  <mat-card-content>
    <table mat-table [dataSource]="runs" style="width:100%;">
      <ng-container matColumnDef="startedAt">
        <th mat-header-cell *matHeaderCellDef>Started</th>
        <td mat-cell *matCellDef="let r">{{ r.startedAt | date:'short' }}</td>
      </ng-container>

      <ng-container matColumnDef="duration">
        <th mat-header-cell *matHeaderCellDef>Duration</th>
        <td mat-cell *matCellDef="let r">{{ duration(r) }}</td>
      </ng-container>

      <ng-container matColumnDef="status">
        <th mat-header-cell *matHeaderCellDef>Status</th>
        <td mat-cell *matCellDef="let r">
          <span [class]="'status-badge ' + statusClass(r.status)">{{ r.status }}</span>
        </td>
      </ng-container>

      <ng-container matColumnDef="jobs">
        <th mat-header-cell *matHeaderCellDef>Jobs</th>
        <td mat-cell *matCellDef="let r">{{ jobs(r) }}</td>
      </ng-container>

      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let r">
          <button mat-button color="primary" (click)="viewRun(r.id)">View →</button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>

    <div *ngIf="runs.length === 0" style="padding: 24px; text-align: center; color: #aaa;">
      No pipeline runs yet.
    </div>
  </mat-card-content>
</mat-card>
```

- [ ] **Step 3: Create dashboard.component.scss** (empty — uses global styles)

```scss
// styles in styles.scss
```

- [ ] **Step 4: Build to verify**

```bash
ng build
```

Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
cd ../../..
git add src/DataExportPlatform.Web/ClientApp/src/app/dashboard/
git commit -m "feat: add DashboardComponent with KPI cards, runs table, and polling"
```

---

## Task 13: RunDetailComponent

**Files:**
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/run-detail/run-detail.component.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/run-detail/run-detail.component.html`

- [ ] **Step 1: Create run-detail.component.ts**

```typescript
// src/app/run-detail/run-detail.component.ts
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { PipelineRunDto } from '../models/api.models';
import { RunsService } from '../services/runs.service';

@Component({
  selector: 'app-run-detail',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule],
  templateUrl: './run-detail.component.html',
})
export class RunDetailComponent implements OnInit {
  run?: PipelineRunDto;
  notFound = false;
  displayedColumns = ['appId', 'fileName', 'recordCount', 'fileSizeBytes', 'status', 'exportedAt', 'errorMessage'];

  constructor(private route: ActivatedRoute, private runsService: RunsService) {}

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.runsService.getRun(id).subscribe({
      next: run => (this.run = run),
      error: err => {
        if (err?.status === 404) this.notFound = true;
      },
    });
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  statusClass(status: string): string {
    return 'status-' + status.toLowerCase();
  }
}
```

- [ ] **Step 2: Create run-detail.component.html**

```html
<!-- src/app/run-detail/run-detail.component.html -->
<div *ngIf="notFound" class="error-message">Run not found.</div>

<div *ngIf="run">
  <h2 style="color:#00205B; margin-top:0;">Run #{{ run.id }}</h2>

  <mat-card style="margin-bottom: 20px;">
    <mat-card-content>
      <table style="border-collapse:collapse; width:100%; font-size:14px;">
        <tr><td style="padding:6px 12px; color:#888; width:140px;">Started</td><td>{{ run.startedAt | date:'medium' }}</td></tr>
        <tr><td style="padding:6px 12px; color:#888;">Finished</td><td>{{ (run.finishedAt | date:'medium') ?? '—' }}</td></tr>
        <tr><td style="padding:6px 12px; color:#888;">Status</td><td><span [class]="'status-badge ' + statusClass(run.status)">{{ run.status }}</span></td></tr>
        <tr *ngIf="run.errorMessage"><td style="padding:6px 12px; color:#888;">Error</td><td style="color:#c62828;">{{ run.errorMessage }}</td></tr>
      </table>
    </mat-card-content>
  </mat-card>

  <h3 style="color:#00205B;">Export Logs</h3>
  <mat-card>
    <mat-card-content>
      <table mat-table [dataSource]="run.exportLogs" style="width:100%;">
        <ng-container matColumnDef="appId">
          <th mat-header-cell *matHeaderCellDef>App</th>
          <td mat-cell *matCellDef="let l">{{ l.appId }}</td>
        </ng-container>
        <ng-container matColumnDef="fileName">
          <th mat-header-cell *matHeaderCellDef>File</th>
          <td mat-cell *matCellDef="let l">{{ l.fileName }}</td>
        </ng-container>
        <ng-container matColumnDef="recordCount">
          <th mat-header-cell *matHeaderCellDef>Records</th>
          <td mat-cell *matCellDef="let l">{{ l.recordCount | number }}</td>
        </ng-container>
        <ng-container matColumnDef="fileSizeBytes">
          <th mat-header-cell *matHeaderCellDef>Size</th>
          <td mat-cell *matCellDef="let l">{{ formatBytes(l.fileSizeBytes) }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let l">
            <span [class]="'status-badge ' + statusClass(l.status)">{{ l.status }}</span>
          </td>
        </ng-container>
        <ng-container matColumnDef="exportedAt">
          <th mat-header-cell *matHeaderCellDef>Exported At</th>
          <td mat-cell *matCellDef="let l">{{ l.exportedAt | date:'short' }}</td>
        </ng-container>
        <ng-container matColumnDef="errorMessage">
          <th mat-header-cell *matHeaderCellDef>Error</th>
          <td mat-cell *matCellDef="let l" style="color:#c62828;">{{ l.errorMessage ?? '' }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
      </table>
    </mat-card-content>
  </mat-card>
</div>
```

- [ ] **Step 3: Build to verify**

```bash
ng build
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
cd ../../..
git add src/DataExportPlatform.Web/ClientApp/src/app/run-detail/
git commit -m "feat: add RunDetailComponent"
```

---

## Task 14: TriggerComponent

**Files:**
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/trigger/trigger.component.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/trigger/trigger.component.html`

- [ ] **Step 1: Create trigger.component.ts**

```typescript
// src/app/trigger/trigger.component.ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PipelineService } from '../services/pipeline.service';

@Component({
  selector: 'app-trigger',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatProgressSpinnerModule],
  templateUrl: './trigger.component.html',
})
export class TriggerComponent {
  running = false;

  constructor(private pipeline: PipelineService, private snackBar: MatSnackBar) {}

  trigger() {
    this.running = true;
    this.pipeline.trigger().subscribe({
      next: res => {
        this.running = false;
        this.snackBar.open(res.message, 'Close', { duration: 5000 });
      },
      error: () => {
        this.running = false;
        // 5xx handled by global error interceptor
      },
    });
  }
}
```

- [ ] **Step 2: Create trigger.component.html**

```html
<!-- src/app/trigger/trigger.component.html -->
<h2 style="color:#00205B; margin-top:0;">Trigger Run</h2>

<mat-card style="max-width: 480px;">
  <mat-card-content>
    <p>Manually trigger a pipeline run. This will run all export jobs synchronously and may take a minute.</p>

    <div style="display:flex; align-items:center; gap:16px; margin-top:16px;">
      <button mat-raised-button
              [disabled]="running"
              style="background:#DA1F2B; color:white;"
              (click)="trigger()">
        ▶ Run Pipeline
      </button>
      <mat-spinner *ngIf="running" diameter="24"></mat-spinner>
      <span *ngIf="running" style="color:#888; font-size:13px;">Running…</span>
    </div>
  </mat-card-content>
</mat-card>
```

- [ ] **Step 3: Build to verify**

```bash
ng build
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
cd ../../..
git add src/DataExportPlatform.Web/ClientApp/src/app/trigger/
git commit -m "feat: add TriggerComponent"
```

---

## Task 15: ArchiveIndexComponent

**Files:**
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/archive-index/archive-index.component.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/archive-index/archive-index.component.html`

- [ ] **Step 1: Create archive-index.component.ts**

```typescript
// src/app/archive-index/archive-index.component.ts
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { ArchiveSummaryDto } from '../models/api.models';
import { ArchiveService } from '../services/archive.service';

@Component({
  selector: 'app-archive-index',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule],
  templateUrl: './archive-index.component.html',
})
export class ArchiveIndexComponent implements OnInit {
  summaries: ArchiveSummaryDto[] = [];

  constructor(private archive: ArchiveService, private router: Router) {}

  ngOnInit() {
    this.archive.getSummaries().subscribe({
      next: s => (this.summaries = s),
    });
  }

  viewJob(appId: string) {
    this.router.navigate(['/archive', appId]);
  }
}
```

- [ ] **Step 2: Create archive-index.component.html**

```html
<!-- src/app/archive-index/archive-index.component.html -->
<h2 style="color:#00205B; margin-top:0;">Archive</h2>

<div *ngIf="summaries.length === 0" style="color:#aaa; padding:16px;">
  No archive directories found.
</div>

<div style="display:flex; gap:16px; flex-wrap:wrap;">
  <mat-card *ngFor="let s of summaries" style="min-width:200px; cursor:pointer;" (click)="viewJob(s.appId)">
    <mat-card-header>
      <mat-card-title style="color:#00205B;">{{ s.appId }}</mat-card-title>
    </mat-card-header>
    <mat-card-content>
      <div style="font-size:13px; line-height:2;">
        <div><strong>{{ s.dayCount }}</strong> day(s)</div>
        <div><strong>{{ s.fileCount }}</strong> file(s)</div>
        <div *ngIf="s.latestDay">Latest: <strong>{{ s.latestDay }}</strong></div>
      </div>
    </mat-card-content>
    <mat-card-actions>
      <button mat-button color="primary">Browse →</button>
    </mat-card-actions>
  </mat-card>
</div>
```

- [ ] **Step 3: Build to verify**

```bash
ng build
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
cd ../../..
git add src/DataExportPlatform.Web/ClientApp/src/app/archive-index/
git commit -m "feat: add ArchiveIndexComponent"
```

---

## Task 16: ArchiveJobComponent

**Files:**
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/archive-job/archive-job.component.ts`
- Create: `src/DataExportPlatform.Web/ClientApp/src/app/archive-job/archive-job.component.html`

- [ ] **Step 1: Create archive-job.component.ts**

```typescript
// src/app/archive-job/archive-job.component.ts
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { ArchiveJobDto } from '../models/api.models';
import { ArchiveService } from '../services/archive.service';

@Component({
  selector: 'app-archive-job',
  standalone: true,
  imports: [CommonModule, MatExpansionModule, MatListModule, MatIconModule],
  templateUrl: './archive-job.component.html',
})
export class ArchiveJobComponent implements OnInit {
  job?: ArchiveJobDto;
  accessDenied = false;
  notFound = false;

  constructor(private route: ActivatedRoute, private archive: ArchiveService) {}

  ngOnInit() {
    const appId = this.route.snapshot.paramMap.get('appId')!;
    this.archive.getJob(appId).subscribe({
      next: job => (this.job = job),
      error: err => {
        if (err?.status === 403) this.accessDenied = true;
        else if (err?.status === 404) this.notFound = true;
      },
    });
  }

  downloadUrl(appId: string, day: string, fileName: string): string {
    return this.archive.buildDownloadUrl(appId, day, fileName);
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
```

- [ ] **Step 2: Create archive-job.component.html**

```html
<!-- src/app/archive-job/archive-job.component.html -->
<div *ngIf="accessDenied" class="access-denied">
  Access denied. You do not have permission to view this archive.
</div>

<div *ngIf="notFound" class="error-message">
  Archive not found.
</div>

<div *ngIf="job">
  <h2 style="color:#00205B; margin-top:0;">{{ job.appId }} Archive</h2>

  <div *ngIf="job.days.length === 0" style="color:#aaa;">No archived files.</div>

  <mat-accordion>
    <mat-expansion-panel *ngFor="let day of job.days">
      <mat-expansion-panel-header>
        <mat-panel-title style="font-weight:600; color:#00205B;">{{ day.day }}</mat-panel-title>
        <mat-panel-description>{{ day.files.length }} file(s)</mat-panel-description>
      </mat-expansion-panel-header>

      <mat-list>
        <mat-list-item *ngFor="let file of day.files">
          <mat-icon matListItemIcon style="color:#00205B;">description</mat-icon>
          <a matListItemTitle
             [href]="downloadUrl(job.appId, day.day, file.fileName)"
             download
             style="color:#00205B; text-decoration:none;">
            {{ file.fileName }}
          </a>
          <span matListItemLine style="font-size:11px; color:#aaa;">{{ formatBytes(file.sizeBytes) }}</span>
        </mat-list-item>
      </mat-list>
    </mat-expansion-panel>
  </mat-accordion>
</div>
```

- [ ] **Step 3: Build to verify**

```bash
ng build
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
cd ../../..
git add src/DataExportPlatform.Web/ClientApp/src/app/archive-job/
git commit -m "feat: add ArchiveJobComponent with day accordion and file downloads"
```

---

## Task 17: Proxy Config and Full Integration Verification

**Files:**
- Create: `src/DataExportPlatform.Web/ClientApp/proxy.conf.json`
- Modify: `src/DataExportPlatform.Web/ClientApp/angular.json` (add proxyConfig)

- [ ] **Step 1: Create proxy.conf.json**

```json
{
  "/api": {
    "target": "http://localhost:49467",
    "secure": false,
    "changeOrigin": false
  }
}
```

- [ ] **Step 2: Add proxyConfig to angular.json**

In `ClientApp/angular.json`, find `projects > ClientApp > architect > serve > options` and add:

```json
"proxyConfig": "proxy.conf.json"
```

- [ ] **Step 3: Production build verification**

```bash
cd src/DataExportPlatform.Web/ClientApp
ng build --configuration production
```

Expected: Build succeeds; `wwwroot/index.html` exists.

- [ ] **Step 4: Verify full dotnet build**

```bash
cd ../../..
dotnet build
```

Expected: All projects build successfully.

- [ ] **Step 5: Run existing tests**

```bash
dotnet test
```

Expected: All existing tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/DataExportPlatform.Web/ClientApp/proxy.conf.json src/DataExportPlatform.Web/ClientApp/angular.json
git commit -m "feat: add Angular dev proxy config and verify production build"
```

---

## Task 18: Cleanup and Final Commit

- [ ] **Step 1: Verify wwwroot is gitignored**

```bash
git status src/DataExportPlatform.Web/wwwroot/
```

Expected: No files listed (gitignored).

- [ ] **Step 2: Verify no Razor page files remain**

```bash
ls src/DataExportPlatform.Web/Pages 2>/dev/null && echo "ERROR: Pages dir still exists" || echo "OK: Pages dir removed"
```

Expected: `OK: Pages dir removed`.

- [ ] **Step 3: Final build + test run**

```bash
dotnet build && dotnet test
```

Expected: All green.

- [ ] **Step 4: Final commit**

```bash
git add -A
git status  # confirm only expected files
git commit -m "feat: Angular 21 frontend complete — replaces Razor Pages"
```
