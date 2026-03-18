# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run (development)
dotnet run --project src/DataExportPlatform.Web

# Run tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~CostcenterResponsibleValidatorTests"

# Add EF migration
dotnet ef migrations add <MigrationName> --project src/DataExportPlatform.Infrastructure --startup-project src/DataExportPlatform.Web

# Apply migrations manually
dotnet ef database update --project src/DataExportPlatform.Infrastructure --startup-project src/DataExportPlatform.Web
```

Local dev runs on `http://localhost:49467`. Start the Docker SQL Server first:
```bash
docker-compose up -d
```

## Architecture

Four projects with strict layering — no upward dependencies:

```
Core (domain) ← Infrastructure (EF Core, stubs)
Core          ← Pipeline (orchestrator, jobs, validators)
Core + Infra + Pipeline ← Web (ASP.NET host, Razor Pages, API)
```

**Core** — pure domain with no external dependencies: models (`Employee`, `Costcenter`, `Accessright`, `PipelineRun`, `ExportLog`, `DataContext`) and interfaces (`IExportJob`, `IDataSource`, `IFileWriter`, `IOverrideValidator<T>`).

**Infrastructure** — EF Core `AppDbContext`, SQL Server migrations, data source stubs (TODO: replace with real sources in production), and `LocalFileWriter`.

**Pipeline** — `PipelineOrchestrator` drives the full export cycle. `DataContextCache` holds the latest data snapshot. Three concrete export jobs live here: `AppAExportJob` (CSV), `AppBExportJob` (JSON), `AppCExportJob` (three CSVs including dynamic holiday calendar).

**Web** — ASP.NET Core host configured as a Windows Service. Razor Pages for the dashboard/CRUD UI. `PipelineController` exposes `POST /api/pipeline/trigger` for external schedulers (no built-in scheduler by design).

## Pipeline Flow

`PipelineOrchestrator.RunAsync()`:
1. Fetch all three data sources in parallel (stubs today; replace with real API/LDAP/ERP calls).
2. Wrap results in an immutable `DataContext` snapshot.
3. Load `CostcenterResponsibleOverride` records from DB and apply them (`DataContext.ApplyOverrides()`).
4. Insert a `PipelineRun` row (status = Running).
5. Execute all `IExportJob` implementations in parallel, each receiving the same `DataContext`.
6. Write one `ExportLog` row per output file; update `PipelineRun` status to Success / PartialFailure / Failed.

## Extending the Platform

**Add an export job** — implement `IExportJob`, register it in `Program.cs` as `IExportJob`, done.

**Replace a data source stub** — implement `IDataSource`, swap the DI registration for `EmployeeSourceStub` / `CostcenterSourceStub` / `AccessrightSourceStub`.

**Change output destination** — implement `IFileWriter` (e.g., Azure Blob, SFTP) and swap `LocalFileWriter` in DI.

## Production Migration Checklist

See `README.md` for the full seven-step checklist. Key points:
- Remove auto-migration from `Program.cs` (development-only guard exists but double-check).
- Replace the three data source stubs with real integrations.
- Update `ConnectionStrings:DefaultConnection` and `ExportSettings:OutputDirectory` in `appsettings.json`.
- Deploy as a Windows Service; trigger via `POST /api/pipeline/trigger` from Task Scheduler, Azure Logic Apps, or cron.

## Configuration

| Setting | Dev (`appsettings.Development.json`) | Prod (`appsettings.json`) |
|---|---|---|
| Connection string | `Server=localhost,1433;…;User Id=sa;Password=Dev_Password1!` | Windows Auth, `localhost` |
| Output directory | `C:\DataExports` | `C:\DataExports` |
| Auto-migrate | Yes (env guard in `Program.cs`) | No |
