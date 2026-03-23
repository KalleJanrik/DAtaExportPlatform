# Export Job Selection on Trigger Run Tab

**Date:** 2026-03-23
**Status:** Approved

## Summary

Add per-job checkboxes to the Angular trigger run tab so users can choose which export jobs to execute. All jobs are pre-checked by default. The API is updated to require an explicit job list — an empty or missing list returns 400.

## Backend Changes

### New endpoint: `GET /api/pipeline/jobs`

Resolves all registered `IExportJob` implementations from the DI container and returns their IDs.

**Response:**
```json
[
  { "appId": "AppA" },
  { "appId": "AppB" },
  { "appId": "AppC" },
  { "appId": "AppD" }
]
```

- Same `[Authorize]` policy as the trigger endpoint.
- No new models needed — inline anonymous object is sufficient.
- All `IExportJob` implementations are registered as `AddTransient` in `Program.cs`, so injecting `IEnumerable<IExportJob>` directly into the controller constructor is safe and consistent with how the orchestrator resolves them.

### Modified endpoint: `POST /api/pipeline/trigger`

**Request body (was empty `{}`, now required):**
```json
{ "jobs": ["AppA", "AppC"] }
```

- Returns `400 Bad Request` if `jobs` is null or empty.
- Returns `400 Bad Request` if any job ID in `jobs` does not match a registered `IExportJob.AppId`, with a message listing the unrecognised IDs (e.g. `"Unknown job IDs: NonExistent"`). The controller resolves the available job IDs via `IEnumerable<IExportJob>` to perform this validation before calling the orchestrator.
- Passes the validated job list to `PipelineOrchestrator.RunAsync()`.

### `PipelineOrchestrator.RunAsync()`

Signature changes from:
```csharp
public async Task RunAsync(CancellationToken ct = default)
```
to:
```csharp
public async Task RunAsync(IReadOnlyCollection<string> jobFilter, CancellationToken ct = default)
```

After resolving all `IExportJob` from DI, filter to only those whose `AppId` is in `jobFilter`:
```csharp
var exportJobs = allJobs.Where(j => jobFilter.Contains(j.AppId)).ToList();
```

No changes to `IExportJob` interface.

## Frontend Changes

### `PipelineService`

Add method:
```typescript
getJobs(): Observable<{ appId: string }[]>
```
Calls `GET /api/pipeline/jobs`.

Update existing method:
```typescript
trigger(jobs: string[]): Observable<{ message: string }>
```
Posts `{ jobs }` as the request body.

### `TriggerComponent`

- On `ngOnInit`: call `getJobs()` and store the list; mark all as selected.
- Track selected job IDs in a `Set<string>` or boolean map.
- Toggle selection on checkbox change.
- On trigger: pass the selected IDs array to `pipeline.trigger()`.
- Disable the "Run Pipeline" button when no jobs are checked (in addition to while running).

### Template

Above the "Run Pipeline" button, render a checkbox list:

```
[ ✓ ] AppA
[ ✓ ] AppB
[ ✓ ] AppC
[ ✓ ] AppD

[ Run Pipeline ]
```

## Data Flow

```
User checks/unchecks jobs
  ↓
"Run Pipeline" clicked (disabled if none selected)
  ↓
POST /api/pipeline/trigger  { jobs: ["AppA", "AppC"] }
  ↓
PipelineController validates jobs not empty → 400 if empty
  ↓
PipelineOrchestrator.RunAsync(["AppA", "AppC"])
  ↓
Filter IExportJob list to AppA + AppC only
  ↓
Execute filtered jobs in parallel (existing logic unchanged)
  ↓
Return 200 { message: "Pipeline run completed successfully." }
  ↓
Snackbar notification
```

## Out of Scope

- Display names for jobs (AppId used directly in the UI)
- Persisting the user's last selection
- Authorization per job
- Recording which jobs were selected on the `PipelineRun` row — a partial-selection run is distinguishable only via its `ExportLog` child rows
