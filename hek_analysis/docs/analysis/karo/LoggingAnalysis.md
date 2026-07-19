# KARO — Logging Analysis

**Summary:** Logging is provided by a small, shared, home-grown flat-file logger (`Logger.dll`) with no structured logging, no correlation IDs, no log levels beyond a coarse category enum, and no redaction — it captures plaintext PHI and credentials by design in several call sites.

## Findings

### Logging framework
- Custom singleton class `Logger.Logging` (`DAL/HSSWebAPI` both depend on the shared `Logger.dll`), not Serilog/NLog/`ILogger`/Application Insights/OpenTelemetry. (`Logger\Logging.cs`)
- Two log "streams": event logs (`WriteEventLog`) and exception logs (`WriteExceptionLog`), each written to a separate directory tree, one file per day, named `dd-MMM-yyyy.txt` (`Logger\Logging.cs` lines 15-17, 122, 161).
- A `LogType` enum (`Logger\TypeEnums.cs`) allows redirecting logs into named subfolders (`Appointment`, `Claims`, `Default`, `Immunization`, `Inbox`, `Lab`, `Outbox`, `Referral`, `Timeline`, `Roster`, `CommonForm`, `ACC18`) by string-replacing `"\IntegrationServices\"` in the base path with `"\<LogType>\"` (`Logger\Logging.cs` lines 116-117, 155-156) — a crude categorization mechanism, not a true log-level system (no Debug/Info/Warn/Error/Critical severity concept).
- `Controllers\APIController.cs` uses only `TypeEnums.LogType.Default` throughout (via its local `WriteLog()` wrapper, lines 2020-2026) — none of the other log categories are exercised by KARO itself, suggesting they exist for the sibling MHN/HL7 systems sharing this Logger.

### What's logged
- **Every** controller action logs entry (`WriteLog("In <ActionName>")`) and, in most actions, a second detailed line with the resolved `patientId`, `encounterId`, `practiceid`, `system`, `pho` (e.g. `Controllers\APIController.cs` line 196, 251, 306, 384, 436...).
- Several actions also log the **full response payload** back out (`WriteLog("In GetConditions: " + result)`, line 270; similar in `GetDemographics` line 346, `GetMedications` line 558, `GetObservations` line 613) — meaning complete clinical data (diagnoses, medications, observations) passes through the flat-file log in plaintext.
- `SaveClinicalNotes` logs the complete narrative note text (subjective/objective/assessment/plan) in the pre-validation log line (`Controllers\APIController.cs` lines 876-877).
- `Authenticate` (both GET and POST) logs the **plaintext password** (lines 62, 106, 132).
- Exceptions are logged via `WriteLog(message, ex)` → `Logging.Instance.WriteExceptionLog(message, ex)`, capturing `ex.Message` and `ex.StackTrace` (`Logger\Logging.cs` lines 106-112) — reasonable for diagnostics, but the accompanying `message` parameter often already contains the same PHI logged elsewhere (e.g. `Controllers\APIController.cs` line 1186-1187, `WriteLog(" SaveSummary Input >> " + result, new Exception())`).

### What's NOT logged
- No structured/security-specific audit log distinguishing "who accessed which patient's data and when" from general debug tracing — access and debug information are interleaved in the same event-log stream.
- No correlation ID / request ID is generated or propagated anywhere in the codebase (no `X-Correlation-Id`, no `Activity`/`TraceId` usage found) — reconstructing a single request's full lifecycle across log lines relies purely on matching timestamps and embedded patient/encounter IDs.
- No structured (JSON) log format — all entries are free-text strings concatenated with `+` (e.g. `Controllers\APIController.cs` line 196), making automated log parsing/SIEM ingestion difficult without custom regex.
- No log level filtering configuration (no way to run "warnings and above only" in production) — every `WriteEventLog` call writes unconditionally regardless of environment.

### Traceability
- `LogRoot` app setting (`ConfigurationManager.AppSettings["LogRoot"]`) determines the base log directory, falling back to the app's `AppDomain.CurrentDomain.BaseDirectory` if unset or inaccessible (`Logger\Logging.cs` lines 38-51) — meaning in a misconfigured environment, logs silently end up inside the web app's own deployment folder, which could be web-accessible depending on IIS configuration.
- No evidence of log shipping to a centralized system (no Application Insights, no ELK/Splunk client libraries referenced in `packages.config`) — logs are local flat files only, which is a poor fit for a horizontally-scaled, multi-location, 10,000-concurrent-user target architecture.

## Evidence
See inline citations above; primary files: `E:\NZTFS\hsswebapi\DevLocal\Logger\Logging.cs`, `E:\NZTFS\hsswebapi\DevLocal\Logger\TypeEnums.cs`, `E:\NZTFS\hsswebapi\DevLocal\HSSWebAPI\Controllers\APIController.cs`.

## Risks
- PHI and credentials in plaintext, unencrypted, unrotated flat files are both a HIPAA/NZ Health Information Privacy Code compliance risk and an operational risk (unbounded disk growth over time, no evidence of purge/retention).
- Lack of correlation IDs will make production troubleshooting and any future distributed-tracing requirement (needed at 10,000-concurrent-user scale, likely multi-instance) very difficult without a rewrite of the logging approach.
- File-based, per-instance logging does not scale horizontally — if KARO (or its replacement) runs on multiple app-server instances, logs are fragmented across machines with no aggregation.

## Recommendations
- Replace with a structured logging framework (Serilog/`ILogger` + a sink such as Application Insights/OpenTelemetry/ELK) in the unified platform, with explicit log levels and mandatory PII/PHI redaction rules enforced at the sink or via structured field exclusion, not developer discipline.
- Introduce correlation/request IDs (e.g., via middleware) so that a single client request's full trace — API layer + DAL calls + downstream errors — can be reconstructed without relying on timestamp proximity.
- Establish and enforce a "never log credentials, never log full clinical narrative text" rule as a code-review/lint gate in the rewrite, since this pattern is systemic (present in nearly every action) rather than a one-off mistake.
