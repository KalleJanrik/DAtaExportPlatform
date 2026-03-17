# DataExportPlatform

An ASP.NET Core 8 platform for scheduling, running, and monitoring data export jobs. It pulls data from configurable sources, applies business-rule overrides, and writes CSV/JSON output files for downstream applications.

---

## Project Structure

```
DataExportPlatform.sln
docker-compose.yml              # Local SQL Server for development
src/
  DataExportPlatform.Core/      # Domain models and interfaces — no dependencies
  DataExportPlatform.Infrastructure/  # EF Core DbContext, data sources, file writing
  DataExportPlatform.Pipeline/  # Export jobs, validators, pipeline orchestrator
  DataExportPlatform.Web/       # ASP.NET Core Razor Pages host + REST trigger
```

### Core
Pure domain layer. No framework dependencies.

| File | Purpose |
|---|---|
| `Models/` | `Employee`, `Costcenter`, `Accessright`, `PipelineRun`, `ExportLog`, etc. |
| `Models/DataContext.cs` | In-memory snapshot of source data passed to every export job |
| `Interfaces/IExportJob.cs` | Contract every export job must implement |
| `Interfaces/IDataSource.cs` | Contract for fetching source data |
| `Interfaces/IOverrideValidator.cs` | Contract for validating override records before saving |

### Infrastructure
Implements the Core interfaces. This is where all external system integrations live.

| File | Purpose |
|---|---|
| `Data/AppDbContext.cs` | EF Core context — pipeline run history, export logs, overrides |
| `Migrations/` | EF Core migrations — applied automatically on startup in Development |
| `Sources/*Stub.cs` | **Stub implementations** — return hardcoded data (replace for production) |
| `FileWriting/LocalFileWriter.cs` | Writes export files to a local directory |

### Pipeline
Orchestration and business logic.

| File | Purpose |
|---|---|
| `PipelineOrchestrator.cs` | `BackgroundService` — runs daily at 02:00, also supports manual triggers |
| `DataContextCache.cs` | Singleton cache of the latest data snapshot, used by the web layer |
| `Jobs/AppAExportJob.cs` | Exports active employees with costcenter info → CSV |
| `Jobs/AppBExportJob.cs` | Exports access rights with employee names → JSON |
| `Jobs/AppCExportJob.cs` | Exports employees, costcenters, and holiday schedule → 3 CSV files |
| `Validators/CostcenterResponsibleValidator.cs` | Validates override emails exist in the employee list |

### Web
ASP.NET Core host with a Razor Pages UI and a REST endpoint.

| File | Purpose |
|---|---|
| `Program.cs` | DI registration, auto-migration on startup (Development only) |
| `Pages/Index.cshtml` | Dashboard — recent pipeline runs and export logs |
| `Pages/Runs/Trigger.cshtml` | Manually trigger a pipeline run |
| `Pages/Runs/Detail.cshtml` | Detail view for a single pipeline run |
| `Pages/Overrides/` | CRUD UI for costcenter responsible overrides |
| `Controllers/PipelineController.cs` | `POST /api/pipeline/trigger` — REST trigger endpoint |
| `appsettings.json` | Production config (Windows Auth SQL Server, output directory) |
| `appsettings.Development.json` | Dev config — overrides connection string for Docker SQL Server |

---

## Running Locally

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Steps

**1. Start the database**
```bash
docker compose up -d
```
This starts SQL Server 2022 on port 1433 with SA password `Dev_Password1!`.

**2. Run the application**
```bash
cd src/DataExportPlatform.Web
dotnet run
```
On first run, EF Core automatically creates the database and applies migrations.

**3. Open the UI**

Navigate to `http://localhost:49467`

---

## Pipeline Flow

Each run (scheduled or manual) executes these steps:

```
1. Fetch data       — all sources fetched in parallel
2. Apply overrides  — costcenter responsible emails overridden from DB
3. Record run       — PipelineRun row inserted with status = Running
4. Run export jobs  — all jobs run in parallel, each writes one or more files
5. Write logs       — one ExportLog row per output file, PipelineRun updated
```

---

## Moving to a Real Implementation

The platform is designed so that only the stub/infrastructure layer needs to change. The pipeline, jobs, and web UI all stay the same.

### 1. Replace the data source stubs

Each stub in `Infrastructure/Sources/` has a `// TODO` comment. Replace the hardcoded lists with real calls to your HR system, LDAP directory, or API.

```csharp
// Before (stub)
public Task<List<Employee>> FetchEmployeesAsync(CancellationToken ct)
{
    return Task.FromResult(new List<Employee> { /* hardcoded */ });
}

// After (real)
public async Task<List<Employee>> FetchEmployeesAsync(CancellationToken ct)
{
    var response = await _httpClient.GetFromJsonAsync<List<EmployeeDto>>("/api/employees", ct);
    return response.Select(dto => dto.ToEmployee()).ToList();
}
```

### 2. Replace the file writer

`LocalFileWriter` writes to a local directory. For production, implement `IFileWriter` to write to your target destination:

- **Azure Blob Storage** — inject `BlobServiceClient`, write to a container
- **SFTP** — use `SSH.NET` or similar
- **Network share** — update `OutputDirectory` in `appsettings.json` to a UNC path

Register your new implementation in `Program.cs`:
```csharp
// Before
builder.Services.AddSingleton<IFileWriter, LocalFileWriter>();

// After
builder.Services.AddSingleton<IFileWriter, AzureBlobFileWriter>();
```

### 3. Update the connection string

`appsettings.json` currently uses Windows Authentication for a local SQL Server instance. Update it to point to your production database:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=prod-sql.example.com;Database=DataExportPlatform;User Id=app_user;Password=...;TrustServerCertificate=False;"
}
```

Use a secrets manager (Azure Key Vault, environment variables, .NET User Secrets) — never commit production credentials.

### 4. Remove auto-migration from startup

`Program.cs` runs `db.Database.Migrate()` on startup in Development mode only. For production, run migrations as part of your deployment pipeline instead:

```bash
dotnet ef database update --project src/DataExportPlatform.Infrastructure --startup-project src/DataExportPlatform.Web
```

### 5. Add a new export job

Implement `IExportJob` and register it in `Program.cs` — the orchestrator picks it up automatically:

```csharp
public class AppDExportJob : IExportJob
{
    public string AppId => "AppD";

    public async Task<IEnumerable<ExportResult>> RunAsync(DataContext ctx, CancellationToken ct)
    {
        // build and write your files here
    }
}
```

```csharp
// Program.cs
builder.Services.AddTransient<IExportJob, AppDExportJob>();
```

### 6. Adjust the schedule

The pipeline runs daily at 02:00 local time. Change this in `PipelineOrchestrator.cs`:

```csharp
private static TimeSpan ComputeDelayUntilNextRun()
{
    var now = DateTime.Now;
    var next = now.Date.AddHours(2); // change this hour
    if (next <= now) next = next.AddDays(1);
    return next - now;
}
```

### 7. Deploy as a Windows Service

The app is already configured with `UseWindowsService()`. To install it:

```bash
dotnet publish -c Release -o ./publish
sc create DataExportPlatform binpath="C:\path\to\publish\DataExportPlatform.Web.exe"
sc start DataExportPlatform
```

---

## Configuration Reference

| Key | Default | Description |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | local SQL Server | EF Core connection string |
| `ExportSettings:OutputDirectory` | `C:\DataExports` | Directory where export files are written |
