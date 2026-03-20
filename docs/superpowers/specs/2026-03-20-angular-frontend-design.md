# Angular 21 Frontend — Design Spec

**Date:** 2026-03-20
**Status:** Approved
**Scope:** Replace Razor Pages UI with an Angular 21 SPA served from the existing ASP.NET Core host.

---

## 1. Goals

- Replace all Razor Pages with an Angular 21 SPA (no Overrides page — dropped by design).
- Keep a single deployable unit: ASP.NET Core serves both the API and the compiled Angular app.
- Enable Windows Authentication and AD group-based archive authorization (currently commented out in `Program.cs`).
- Add live dashboard updates: poll every 5 seconds while any run is in a `Running` state.

---

## 2. Out of Scope

- CostcenterResponsibleOverride CRUD (Overrides page) — removed entirely.
- SignalR / WebSockets — polling is sufficient.
- New backend business logic — API surface changes only.
- Angular unit tests — no frontend test suite in this project.

---

## 3. Architecture

### 3.1 Hosting (static files, no SPA middleware)

```
src/DataExportPlatform.Web/
  ClientApp/            ← Angular 21 project (ng new, standalone components)
    src/app/
    angular.json        ← outputPath: "../wwwroot"
    proxy.conf.json     ← dev: /api → http://localhost:49467
  wwwroot/              ← ng build output (gitignored); clear before first ng build
  Controllers/
    PipelineController.cs   (update: add [Authorize], remove try/catch — see §4.2)
    RunsController.cs       (new)
    ArchiveController.cs    (new)
    AuthController.cs       (new — whoami endpoint)
  Program.cs            ← updated (see §3.2)
```

**`wwwroot/` state:** Clear any existing contents before running `ng build` for the first time. The Angular build will populate it completely.

`wwwroot/` and `ClientApp/node_modules/` are added to `.gitignore` (see §10).

### 3.2 Program.cs Changes

Apply all changes in this order:

**1. Add `AddProblemDetails()`** — required for the parameterless `UseExceptionHandler()` to return RFC 7807 JSON:

```csharp
builder.Services.AddProblemDetails();
```

**2. Enable Windows Authentication** — uncomment the existing commented-out block (lines 66–71):

```csharp
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddScoped<IClaimsTransformation, AdGroupClaimsTransformation>();
```

**3. Add `JsonStringEnumConverter`** so enum values serialize as strings:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

**4. Replace Razor Pages** — remove `AddRazorPages()`. Delete `Pages/` directory.

**5. Update `Properties/launchSettings.json`:**

```json
"windowsAuthentication": true,
"anonymousAuthentication": false
```

**6. Replace production exception handler** — `Pages/Error.cshtml` is deleted. Update the existing `if/else` block in `Program.cs`; keep the developer exception page in development:

```csharp
if (app.Environment.IsDevelopment())
{
    // keep existing: auto-migration
    app.UseDeveloperExceptionPage();  // add this — shows full stack trace in dev
}
else
{
    app.UseExceptionHandler();  // returns RFC 7807 ProblemDetails JSON on 5xx; requires AddProblemDetails()
}
```

**7. Final authoritative middleware pipeline** — replace the existing routing/mapping block entirely:

```csharp
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();    // must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
```

### 3.3 Authorization

All API controllers are decorated with `[Authorize]` at the class level. The effective authorization matrix:

| Endpoint | Requirement |
|----------|-------------|
| `GET /api/auth/whoami` | Authenticated (any domain user) |
| `GET /api/runs` | Authenticated |
| `GET /api/runs/{id}` | Authenticated |
| `POST /api/pipeline/trigger` | Authenticated |
| `GET /api/archive` | Authenticated; returns summaries for known AppIds with existing directories |
| `GET /api/archive/{appId}` | `Archive.{appId}` policy — enforced imperatively (see §4.4) |
| `GET /api/archive/{appId}/{day}/{file}` | `Archive.{appId}` policy — enforced imperatively (see §4.4) |

> **Deliberate change:** The existing `Archive/Index.cshtml.cs` is unauthenticated. `GET /api/archive` now requires authentication — intentional, as Windows Auth is being enabled globally.

### 3.4 Development Workflow

Run two processes concurrently:

```bash
# Terminal 1 — backend
dotnet run --project src/DataExportPlatform.Web

# Terminal 2 — Angular dev server with API proxy
cd src/DataExportPlatform.Web/ClientApp
ng serve
```

**`proxy.conf.json`:**

```json
{
  "/api": {
    "target": "http://localhost:49467",
    "secure": false,
    "changeOrigin": false
  }
}
```

Reference in `angular.json`:
```json
"serve": { "options": { "proxyConfig": "proxy.conf.json" } }
```

> **NTLM dev limitation:** The webpack-dev-server proxy does not persist TCP connections between requests, which breaks NTLM's multi-round-trip challenge. If Windows Auth fails through `ng serve`, test API calls directly against `http://localhost:49467` in the browser or via a REST client. For production verification, deploy to IIS/Windows Service as usual — Windows Auth works correctly in that context.

### 3.5 Production Build

In production, the Angular app and API share the same origin — required for `<a download>` links (§4.4) and Windows Auth.

```bash
cd src/DataExportPlatform.Web/ClientApp
ng build --configuration production
dotnet publish src/DataExportPlatform.Web
```

---

## 4. API Layer

All responses use plain DTO records. All endpoints require Windows Auth — Angular sends `withCredentials: true` via an interceptor (§5.5).

> **Windows Auth 401 note:** With Negotiate/NTLM, the browser handles the challenge/response handshake transparently. Angular will not normally see a `401`. If one surfaces (definitive auth failure), the error interceptor treats it as an access error.

### 4.1 New — AuthController

| Method | Route | Response |
|--------|-------|----------|
| GET | `/api/auth/whoami` | `200`: `{ "username": "DOMAIN\\evang" }` — reads `User.Identity?.Name` |

### 4.2 Existing — PipelineController (modify)

Changes: add `[Authorize]`; **remove the existing try/catch** so unhandled exceptions bubble to `app.UseExceptionHandler()` and return consistent ProblemDetails. The controller body becomes a single `await _orchestrator.RunAsync()` call with no exception handling.

| Method | Route | Response |
|--------|-------|----------|
| POST | `/api/pipeline/trigger` | `200`: `{ "message": "Pipeline run completed successfully." }` · `500`: ProblemDetails (from middleware) |

`TriggerComponent` displays `response.message` from the `200` body in a MatSnackBar. The `500` is handled by the global error interceptor (`error.error?.detail`).

### 4.3 New — RunsController

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/runs` | Last 10 `PipelineRun` records eager-loaded (`.Include(r => r.ExportLogs)`), ordered `StartedAt DESC`. Uses `AppDbContext` directly. Returns `[]` when no runs exist. |
| GET | `/api/runs/{id}` | Single run, eager-loaded with `.Include(r => r.ExportLogs)`. Returns `404 NotFound()` (empty body — ProblemDetails default) if the ID is not found. |

Both endpoints return the same DTO (intentional — 10 × N export logs is an acceptable payload):

```csharp
record PipelineRunSummaryDto(
    int Id,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string Status,           // "Running" | "Success" | "PartialFailure" | "Failed"
    string? ErrorMessage,
    IEnumerable<ExportLogDto> ExportLogs);

record ExportLogDto(
    string AppId,
    string FileName,
    int RecordCount,
    long FileSizeBytes,
    string Status,           // "Success" | "Failed"
    DateTime ExportedAt,
    string? ErrorMessage);
    // ExportLog.Id intentionally excluded — not needed by the UI
```

### 4.4 New — ArchiveController

Reads filesystem via `IConfiguration` using key `ExportSettings:ArchiveRoot`. This key is already present in both `appsettings.json` (`C:\DataExports\Archive`) and `appsettings.Development.json`. Use the same null-coalescing fallback as the existing code: `configuration["ExportSettings:ArchiveRoot"] ?? @"C:\DataExports\Archive"`. `ExportArchiveService` is not used (purge-only, no browsing methods). All export jobs (AppA, AppB, AppC, AppD) produce only `.csv` or `.json` files — the `application/octet-stream` fallback is a safety net only.

**Valid AppIds:** `["AppA", "AppB", "AppC", "AppD"]` (case-insensitive). Return `404` for any other value on `/{appId}` routes.

**Source of truth for `GET /api/archive`:** Iterate the hard-coded AppId list; include only those where the directory exists under `ArchiveRoot`. This is an intentional divergence from the existing Razor page (which enumerates all directories dynamically) — it keeps listing consistent with the known-job validation and prevents phantom directories appearing in the UI.

**Dynamic policy authorization:** `[Authorize]` alone cannot express `Archive.{appId}` since the policy name depends on the route value. Enforce authorization imperatively in each action:

```csharp
var authResult = await _authorizationService.AuthorizeAsync(User, null, $"Archive.{appId}");
if (!authResult.Succeeded) return Forbid();
```

This mirrors the existing `Job.cshtml.cs` pattern.

**Path traversal guard:** For `GET /api/archive/{appId}/{day}/{file}`, validate that **all three** route segments contain neither `/` nor `\`. Return `BadRequest()` if any segment fails. Note: this error cannot be intercepted by Angular (it is triggered by an `<a href>` browser navigation, not `HttpClient`). The `400` will surface as a browser error page. This is acceptable — path traversal inputs will not occur in normal use.

**File downloads:** Use `return File(stream, contentType, fileName)`. Content-Type: `.csv` → `text/csv`, `.json` → `application/json`, otherwise `application/octet-stream`. Pass the bare `fileName` directly — ASP.NET Core sets `Content-Disposition: attachment; filename*=UTF-8''<encoded>` automatically.

**Angular download links:** Use `<a [href]="downloadUrl" download>`. Requires same-origin in production (§3.5). The webpack-dev-server proxy intercepts `<a href>` navigations in dev.

| Method | Route | Response codes |
|--------|-------|----------------|
| GET | `/api/archive` | `200` (array, may be empty) |
| GET | `/api/archive/{appId}` | `200` · `403` (policy) · `404` (directory not found or unknown appId) |
| GET | `/api/archive/{appId}/{day}/{file}` | `200` (file stream) · `400` (path traversal) · `403` (policy) · `404` (file not found) |

**DTOs:**

```csharp
record ArchiveSummaryDto(string AppId, int DayCount, int FileCount, string? LatestDay);
record ArchiveJobDto(string AppId, IEnumerable<DayGroupDto> Days);
record DayGroupDto(string Day, IEnumerable<ArchivedFileDto> Files);
record ArchivedFileDto(string FileName, long SizeBytes);
```

---

## 5. Angular App

### 5.1 Setup

Angular 21 uses standalone components by default. Do **not** pass `--standalone=false`.

```bash
cd src/DataExportPlatform.Web
ng new ClientApp --routing --style=scss
cd ClientApp
ng add @angular/material
```

`HttpClient` provided via `provideHttpClient(withInterceptors([...]))` in `app.config.ts`.

### 5.2 Routing

| Path | Component | Notes |
|------|-----------|-------|
| `/` | `DashboardComponent` | — |
| `/runs/:id` | `RunDetailComponent` | Shows inline "Run not found" on `404` |
| `/trigger` | `TriggerComponent` | — |
| `/archive` | `ArchiveIndexComponent` | — |
| `/archive/:appId` | `ArchiveJobComponent` | Shows inline "Access denied" on `403` (no snackbar — see §5.5) |
| `**` | redirect to `/` | — |

### 5.3 Component Structure

```
AppComponent              Shell: MatSidenav (220px, fixed) + MatToolbar
  ├── DashboardComponent  KPI summary cards + runs table (last 10, no pagination); drives polling
  ├── RunDetailComponent  Single run header + export log table
  ├── TriggerComponent    Trigger button + MatSnackBar showing response.message on success
  ├── ArchiveIndexComponent  Job summary cards (AppId, day count, file count, latest day)
  └── ArchiveJobComponent    Day accordion (MatExpansionPanel) + file list + <a href download> links
```

**Sidenav nav items:** Dashboard (`/`), Archive (`/archive`), divider, Trigger Run (`/trigger`). Active route: red left border. Windows username from `AuthService` at the bottom.

### 5.4 Services

| Service | Responsibility |
|---------|---------------|
| `AuthService` | `GET /api/auth/whoami`; exposes `username$: Observable<string>` |
| `RunsService` | `GET /api/runs`, `GET /api/runs/:id` |
| `PipelineService` | `POST /api/pipeline/trigger` |
| `ArchiveService` | `GET /api/archive`, `GET /api/archive/:appId` |
| `PollingService` | Exposes `runs$: Observable<PipelineRunSummaryDto[]>` and `isPolling$: BehaviorSubject<boolean>` (initialized to `false`) |

### 5.5 Interceptors

```typescript
provideHttpClient(withInterceptors([credentialsInterceptor, httpErrorInterceptor]))
```

**`credentialsInterceptor`:**
```typescript
export const credentialsInterceptor: HttpInterceptorFn = (req, next) =>
  next(req.clone({ withCredentials: true }));
```

**`httpErrorInterceptor`** behavior:

| Status | Behavior |
|--------|----------|
| `401` | MatSnackBar: "You are not authorized." Re-throw. |
| `403` | Re-throw **only** (no snackbar). The calling component shows an inline message. |
| `404` | Re-throw only (no snackbar, no navigation). Component handles inline. |
| `5xx` | MatSnackBar: `error.error?.detail ?? error.message`. |
| Any (with `X-Silent-Error: true` header) | Swallow silently — no snackbar. |

Snackbars auto-dismiss after 5 seconds.

---

## 6. Theme — Handelsbanken Colors

Angular Material 21 uses the MDC token-based API. The v14 `define-palette` / `define-light-theme` API is removed.

**`src/styles.scss`:**

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
```

| Token | Value | Usage |
|-------|-------|-------|
| Primary | `#00205B` | Sidenav, toolbar, active nav border |
| Tertiary | `#DA1F2B` | Trigger button, active nav indicator, error badges |
| Background | `#FFFFFF` | Content area |
| Surface | `#F5F5F5` | Table headers, card backgrounds |

---

## 7. Real-Time Polling

`PollingService`:
- `isPolling$`: `BehaviorSubject<boolean>` initialized to `false`
- `runs$`: `Observable<PipelineRunSummaryDto[]>`

**Pseudo-code:**

```
subscribe():
  isPolling$.next(false)
  fetch /api/runs → emit on runs$          ← initial fetch uses normal error handling (NOT silent)
  if fetch fails → surface error via normal httpErrorInterceptor (snackbar); do not start interval
  if any run.status === 'Running':
    isPolling$.next(true)
    interval(5000) pipe switchMap → fetch /api/runs (X-Silent-Error: true) → emit on runs$
                   pipe takeWhile → continue if any run.status === 'Running'
    on complete: isPolling$.next(false)
```

> **Deliberate scope:** Polling activates only if a run is already in `Running` state at page load. If a run starts in another session while the user is on `/`, they will not see live updates — a page navigation away and back (or manual refresh) is required. This is intentional for simplicity.

`DashboardComponent` subscribes in `ngOnInit`, unsubscribes in `ngOnDestroy`. Navigating away and back restarts the cycle.

> **Normal trigger flow:** `POST /api/pipeline/trigger` is synchronous — by the time Angular receives the response, the run is terminal. Polling does not activate after a manual trigger in normal circumstances.

---

## 8. Error Handling Summary

| Scenario | Behavior |
|----------|----------|
| `401` | MatSnackBar: "You are not authorized." |
| `403` on `GET /api/archive/{appId}` | `ArchiveJobComponent` inline: "Access denied." No snackbar. |
| `404` on `GET /api/runs/{id}` | `RunDetailComponent` inline: "Run not found." |
| `404` on `GET /api/archive/{appId}` | `ArchiveJobComponent` inline: "Archive not found." |
| `400` on file download (path traversal) | Browser-level error (not catchable via `<a href>`). No inline Angular message. |
| `5xx` | MatSnackBar: ProblemDetails detail. |
| Polling failure | Silently swallowed (`X-Silent-Error: true`). |
| `200` on `POST /api/pipeline/trigger` | MatSnackBar: `response.message`. |

---

## 9. Files to Delete / Modify

| Action | Target |
|--------|--------|
| Delete | `src/DataExportPlatform.Web/Pages/` (entire directory) |
| Modify | `Program.cs` — per §3.2 |
| Modify | `Properties/launchSettings.json` — enable Windows Auth |
| Modify | `PipelineController.cs` — add `[Authorize]`, remove try/catch |
| Keep | `src/DataExportPlatform.Web/Authorization/` — still used by `ArchiveController` |

---

## 10. .gitignore Additions

```
src/DataExportPlatform.Web/wwwroot/
src/DataExportPlatform.Web/ClientApp/node_modules/
src/DataExportPlatform.Web/ClientApp/dist/
.superpowers/
```
