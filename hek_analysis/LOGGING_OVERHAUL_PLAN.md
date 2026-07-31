# Plan: Full Per-System Logging Overhaul (all endpoints, technical + readable + errors, host folder)

*Saved into the project per user request, 2026-07-29. **Implemented and verified live 2026-07-29** — see `docs/ai_usage_log.md` for the execution entry and `logs/{hiso,karo,erms,col}/` for real output.*

## Status: Done

All 12 files verified live across all 4 systems (real calls + the 43-test integration suite generated real entries in every readable/technical/errors file). The centralized approach in `LegacyOperationObserver` turned out to cover **far more than the original 5-7 endpoint scope** — since every legacy-compat endpoint already funnels through it, KARO alone produced call-log lines for ~15 different operations (GetClinicalNotes, SaveInvoice, GetDocuments, SaveDocument, GetDemographics, etc.) with zero additional per-endpoint code. Endpoints that don't yet have routing resolution added (most of the ~54 outside the original FR-12 pilot) honestly log "routing could not be determined" rather than inventing a DB server/environment - closing that gap is the one remaining piece of work, done per-handler as time allows, not a blocker to the logging infrastructure itself.

**2026-07-29 follow-up: full endpoint coverage + visible CorrelationId + verbose diagnostic toggle - also done.**

- **All ~45 previously-uncovered endpoints now resolve real routing** (KARO 20, ERMS 19, COL 1 truly remaining + 4 already-wired via shared pipeline, HISO 5) - achieved with far less than 45 individual edits by finding and extending the shared pipeline classes each system already had (`KaroPipeline`, `ErmsReadPipeline`, `ColReadPipeline`) plus the shared controller helper methods (`BuildContext` in KaroCompatController, `Render` in ErmsCompatController, `RenderList` in ColCompatController already existed). HISO's 5 remaining endpoints needed a new `enrichContext` hook added to `LegacyOperationObserver.ObserveAsync` since routing there is only known *after* the mediator call resolves the session - not knowable at context-build time like KARO/ERMS/COL.
- **Fixed the known `Routing.DbName`/`Routing.PracticeId` gap** everywhere (including the original 7 FR-12 pilot endpoints) - readable/technical lines now show the real database name instead of "-".
- **CorrelationId now visible in the readable file itself** (`[{CorrelationId}]` in the output template), not just the technical/errors JSON - a reader can point at one readable-file line and find its exact technical-file counterpart by that ID. `LogsController`/`LogsPanel.tsx` updated to parse and surface it too.
- **`VerboseDiagnosticLogging` toggle added and fully wired** (`VerboseDiagnosticLoggingOptions`, default OFF) - when enabled, every legacy-compat endpoint's full request context (patient/encounter/routing/fields-returned) and full response object (real field values, e.g. tokens/demographic data) are logged to `technical-*.log` only, via `LegacyOperationObserver.LogVerbosePayload`, called automatically from the shared `Tag`/`RecordExpectedFailure`/`LogSuccess`/`ObserveAsync`/`ObserveSwallowedAsync` methods - the same "extend the shared helper, not every endpoint" trick used for the routing rollout, so this covers all ~61 endpoints with only a handful of touch points (the KARO/ERMS/COL shared controller helpers + ~10 standalone Authenticate/SaveDocument/SaveInvoice call sites; HISO's 6 endpoints needed zero controller changes since `ObserveAsync` already had the result in scope). Verified live: toggle OFF produces zero verbose lines anywhere; toggle ON produces the full request/response JSON in `technical-*.log` only (confirmed zero leakage into `readable-*.log`/`errors-*.log`); toggle back OFF reverts immediately on next request. 43/43 live integration tests still pass with the toggle at its default (OFF).
- Verified live: a deliberately-triggered COL `GetSessionData` failure (real legacy bug - empty stored-procedure name) now shows full routing on its error line too (a pre-existing catch-block gap in `ColReadPipeline` fixed along the way). Full 43-test live integration suite still 43/43 after all changes.

## Context
Grew out of the "Logs" tab work (KARO Authenticate/GetDemographics pilot, `[CallLog]` readable lines + a dashboard "Logs" tab). User wants this extended from 2 endpoints to **every legacy-compat endpoint across all 4 systems** (~61 total, not just the original 7-endpoint FR-12 pilot), wants logs on a **real host folder** (not a Docker volume, so they're directly browsable/attachable in Windows Explorer), wants **both a technical file and a plain-English file** per system so a non-technical read is possible but full diagnostic detail isn't lost, and a **separate errors file per system** so problems are easy to find and easy for the user to hand to Claude for fast diagnosis later. Full design was talked through and agreed on with the user across several messages; this is the consolidated, final version of that discussion.

## Final agreed design

**Folder layout** (host-mounted, not a Docker volume — real path `E:\claude_projects\HEK Core API\logs\`):
```
logs/
  hiso/   → technical-.log, readable-.log, errors-.log
  karo/   → technical-.log, readable-.log, errors-.log
  erms/   → technical-.log, readable-.log, errors-.log
  col/    → technical-.log, readable-.log, errors-.log
```
12 files total. Each rolls daily and gets a date suffix Serilog appends automatically (`technical-20260729.log`); if a file hits its size cap the same day, Serilog appends a sequence number (`technical-20260729_001.log`, `_002.log`, ...) — no manual naming needed, this is native `Serilog.Sinks.File` behavior (`rollingInterval: Day` + `rollOnFileSizeLimit: true`).

**Three files per system, each with a distinct purpose:**
- `technical-*.log` — full detail, **JSON-structured** (via `Serilog.Formatting.Compact`'s `CompactJsonFormatter`, a package already compatible with the pinned `Serilog.Sinks.File` version) rather than plain text — easier and more precise to hand to Claude for fast triage (exact field/value pairs, no ambiguous parsing). Retention: 30 days.
- `readable-*.log` — only the plain-English `[CallLog]` sentences (field **names**, never values — PHI must not land in plain text, per the project's existing NZ HIPC stance). Retention: 30 days.
- `errors-*.log` — only Warning/Error-level events for that system (auth failures, exceptions, expected-failure business errors). Retention: **60-90 days** (rarer, more valuable for later troubleshooting than routine traffic).

**Correlation across files:** every log line for a given request carries the same short **Request ID** (reuse `Activity.Current?.TraceId` if already flowing via OpenTelemetry, or a lightweight `Guid`/`string` minted once per request and pushed via `LogContext.PushProperty("RequestId", ...)` in middleware) — so a readable-file entry ("KARO Authenticate failed for patient X") can be matched to its exact technical-file entry (full exception/stack trace) by that ID, without guessing by timestamp.

**Covering all ~61 endpoints without touching each one by hand:** instead of repeating the `_logger.LogInformation("[CallLog] ...")` call in every controller action (as done for the KARO pilot), centralize it inside `LegacyOperationObserver` (`src/Api/Telemetry/LegacyOperationObserver.cs`) — every legacy-compat endpoint already funnels through `ObserveAsync`/`ObserveSwallowedAsync`/`Tag`/`RecordExpectedFailure`. Extend those methods to also emit the `[CallLog]` readable line and route to the right per-system sink automatically (system name is already a parameter on every one of those calls). This turns "touch 61 controller actions" into "touch one shared class" — the only remaining per-endpoint work is making sure routing info (`CallRoutingInfo`) is available in the context dict wherever it isn't already (the 7 FR-12 pilot endpoints already have it; the rest don't yet resolve `PracticeRoute`/`HisoSessionRoute` at all, so that resolution needs to be added per-repository/handler, same pattern as the pilot — this is the one part of the work that can't be fully centralized, since routing resolution itself happens per-system).

**Serilog config approach for per-system routing:** use `Serilog.Sinks.File` with `Filter.ByIncludingOnly` sub-loggers (`WriteTo.Logger` blocks in `appsettings.json`, one per system, each filtering on the `System` enrichment property already pushed by `LegacyOperationObserver.TagActivity`/logging calls) rather than 12 hand-wired sinks in code — keeps the pattern declarative and consistent with how `Cors`/`RateLimit`/etc. are already configured in this project.

## Files to touch (representative, not exhaustive — pattern repeats per system/endpoint)
- `appsettings.json` — replace the single `Serilog.WriteTo.File` entry with 12 sub-logger blocks (4 systems × 3 file kinds), each with its own retention.
- `docker-compose.yml` — bind mount `./logs:/app/logs` (replacing the `api-logs` named volume), remove `api-logs` from top-level `volumes:`.
- `.gitignore` — add `logs/` (real patient/encounter IDs must never be committed).
- `src/Api/Telemetry/LegacyOperationObserver.cs` — centralize the `[CallLog]` emission + per-system routing property + Request ID correlation here.
- Per-endpoint routing resolution (same pattern as the FR-12 pilot: `ITenantRegistryService.ResolveRouteAsync`/`IHisoSessionRegistryRepository.FindAsync`) needs adding to every KARO/ERMS/COL/HISO handler that doesn't already have it — this is the bulk of the actual endpoint-by-endpoint work, done handler-by-handler following the exact pattern already proven on the 7 pilot endpoints.
- `src/Api/Features/Admin/Controllers/LogsController.cs` — extend to read per-system readable files (currently reads one hardcoded-pattern file).
- `frontend/src/LogsPanel.tsx`/`App.tsx` — optionally add a per-system filter/tab within the Logs view once multiple systems' entries are flowing.

## Verification
- Confirm all 12 files appear under `E:\claude_projects\HEK Core API\logs\{system}\` after rebuild, directly openable in Notepad/Explorer (no `docker exec` needed).
- Confirm a deliberately-triggered failure (e.g. wrong password) produces matching entries in that system's `errors-*.log` and `technical-*.log` with the same Request ID, and a corresponding line in `readable-*.log`.
- Spot-check several endpoints across all 4 systems (not just KARO) to confirm the centralized `LegacyOperationObserver` change covers them without individual wiring.
- Full 43-test live integration suite still 43/43 after the change (no legacy-compat response body altered — this is purely additive logging).

---

## Prior related work (context, already done)

**"Logs" dashboard tab** — `GET /admin/logs/recent`, `frontend/src/LogsPanel.tsx`, nav entry in `App.tsx`. Currently reads one flat log file; will need extending to the per-system folder layout above.

**30-day bounded retention + Docker persistence** — `retainedFileTimeLimit`/`fileSizeLimitBytes`/`rollOnFileSizeLimit` in `appsettings.json`, `api-logs` Docker volume in `docker-compose.yml` (to be replaced by the host bind-mount above), `Dockerfile` `chown hek:hek /app/logs` fix (same fix likely needed again for the bind-mount folder).

**FR-12 call-flow traceability pilot** — `CallRoutingInfo`, `RoutingSummaryFormatter`, `RoutingHeaderWriter`, `X-Hek-Routing-*` response headers, "Call Flow" card in the frontend — piloted on KARO `Authenticate`/`GetDemographics`, ERMS `Authenticate`/`GetPatientData`, COL `Authenticate`/`GetCurrentPatientData`, HISO `getData`. The routing-resolution part of this pattern is what needs replicating to the other ~54 endpoints for the full logging overhaul above.
