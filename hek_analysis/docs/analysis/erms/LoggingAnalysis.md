# ERMS Web API — Logging Analysis

## Summary
Logging is handled by a small, hand-rolled singleton (`Logger.Logging`) that writes plaintext, date-named flat files to a local `Logs\` directory; it has no structured format, no log levels beyond a coarse category enum, no correlation IDs, no rotation/retention policy, and (per SecurityAnalysis.md SEC-06) logs sensitive data including bearer tokens and patient identifiers.

## Findings

### Logging framework
- Custom project `Logger` (`Logger.dll`), classes `Logging` (singleton via `Logging.Instance`), `Utility`, `TypeEnums`. Not Serilog/NLog/log4net/ILogger — though `NLog` is separately referenced (and imported but apparently unused for actual logging calls) in `DAL/DMSAWS/DMSAWS.cs`, meaning **two different logging mechanisms coexist in the same solution** without integration.
- Evidence: `E:\NZTFS\ermsapi\DevLocal\Logger\Logging.cs`, `E:\NZTFS\ermsapi\DevLocal\DAL\DMSAWS\DMSAWS.cs` (`using NLog;`).

### Where logs are written
- Two categories, each a static path relative to `AppDomain.CurrentDomain.BaseDirectory` (or `AppSettings["LogRoot"]` if configured): `Logs\IntegrationServices\ExceptionLogs\` and `Logs\IntegrationServices\EventLogs\`, one file per day named `dd-MMM-yyyy.txt`. A `TypeEnums.LogType` value (e.g. `Appointment`, `Claims`, `Referral`, `Roster`, `ACC18`, `Default`) can redirect the write into a differently-named subfolder by string-replacing `"IntegrationServices"` in the path at call time. ERMS's own calls all pass `TypeEnums.LogType.Default`, so all ERMS logs land in the generic `IntegrationServices` folders alongside logs from any other module sharing this `Logger` project.
- Evidence: `E:\NZTFS\ermsapi\DevLocal\Logger\Logging.cs` lines 15-18, 106-179; `E:\NZTFS\ermsapi\DevLocal\Logger\TypeEnums.cs`.

### What is logged
- **Every** controller action logs an entry/parameters line and a "ResponseResult" line (full serialized XML/JSON of the response) on both `APIController` and `COLController` — e.g. `WriteLog("In GetPatientData: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid);` and later `WriteLog("In GetPatientData: ResponseResult : " + result.ToString());`.
- Exceptions are logged with a message + `ex.Message` + full `ex.StackTrace` via `WriteExceptionLog`.
- `ERMSAPIProxy` logs the full outgoing request including the `Authorization` header and request body when forwarding to the Azure mirror (SecurityAnalysis.md SEC-06).
- `SaveDocument` explicitly logs `"DEBUG: SaveDocument: " + result` — the **raw incoming XML payload including the base64 document content** — before any parsing, at least in one place (line 1551, `Controllers/APIController.cs`); other document-content fields are scrubbed later in a `finally` block for the *response* object only, not for what was already written to the log.

### What is not logged
- No structured/queryable fields (everything is a single free-text line); no request duration/performance metrics; no correlation/trace/request ID tying a request's entry, exception, and response-result lines together beyond textual proximity in the same file; no user/identity field beyond whatever patient/encounter ids happen to be in the message text; no log level filtering (everything is written unconditionally — there is no way to run "warn and above only" in production).

### Error handling around logging itself
- All file I/O in `Logging.cs` is wrapped in empty `catch { }` blocks (e.g., `CreateLoggingFiles()`, `WriteExceptionLog()`, `WriteEventLog()`), so if the log directory is unwritable (permissions, disk full, path issue), logging **silently fails** with no fallback and no alert — the application continues serving requests with logging effectively (and invisibly) disabled.
- Evidence: `E:\NZTFS\ermsapi\DevLocal\Logger\Logging.cs` lines 47, 83, 124-141, 153-178 (all wrapped in bare `try/catch { }`).

### Correlation / traceability
> Unable to verify from available source: there is no request/correlation ID generated or propagated anywhere in the reviewed controllers, DAL, or Logger code. Matching a specific client-visible error to its corresponding log lines depends entirely on timestamp + patient/encounter id text-matching across the day's flat file.

## Evidence
- `E:\NZTFS\ermsapi\DevLocal\Logger\Logging.cs`
- `E:\NZTFS\ermsapi\DevLocal\Logger\TypeEnums.cs`
- `E:\NZTFS\ermsapi\DevLocal\Logger\Utility.cs`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Controllers\APIController.cs`, `COLController.cs`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Helpers\ERMSAPIProxy.cs`

## Risks
- Silent logging failure (empty catch blocks) means an operator cannot rely on the absence of errors in logs as evidence nothing went wrong — the logger itself could be failing.
- No rotation/retention policy means `Logs\` will grow unbounded over the application's lifetime (one file per category per day, forever) with no evident cleanup job in this codebase.
- Sensitive data in logs (SecurityAnalysis.md SEC-06) compounds with "no access control beyond filesystem permissions" and "no retention limit" into a long-lived, growing repository of PHI-adjacent data and live tokens.
- Two coexisting, non-integrated logging mechanisms (`Logger` project + stray `NLog` usage in `DMSAWS.cs`) will complicate any centralized-logging/observability rollout during migration.

## Recommendations
- Replace with a structured logging framework (Serilog/OpenTelemetry) in the unified platform, with: correlation/trace IDs per request, field-level redaction for identifiers/tokens, configurable log levels, and centralized aggregation (not per-instance flat files).
- Do not port the "log full response body" and "log full Authorization header" patterns forward under any circumstances.
- If any interim operation of the legacy system continues, fix the silent-failure logging bug (empty catch blocks) so operators can detect logging outages, and impose a retention/rotation policy on the `Logs\` directory.
