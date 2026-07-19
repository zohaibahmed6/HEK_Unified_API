# HISO — Logging Analysis

**Summary:** Logging is pervasive but manual and inconsistent, built on a proprietary
`Logger.dll` singleton with two methods (event log, exception log); there is no correlation
ID, no structured logging, and no distinct audit/security-event log — everything goes through
the same free-text event log.

## Findings

### Logging framework/mechanism
A proprietary in-house library, `Logger.dll` (referenced via `HintPath=..\..\PMSdll\Logger.dll`
in `Hiso.csproj`), accessed as a static singleton: `Logger.Logging.Instance.WriteEventLog(string)`
and `Logger.Logging.Instance.WriteExceptionLog(Exception[, string])`. No standard framework
(Serilog, NLog, `ILogger`, Application Insights, OpenTelemetry) is used. **Unable to verify
from available source** what the underlying sink is (file, DB, event log, etc.) since
`Logger.dll`'s source is not included in this project.

Configuration hints at file-based logging: `Web.config` appSettings `LogRoot`
(`D:\Publish`) and `SoapMessageLogRoot` (`C:\Logs\soapmessages.txt`) — both are hardcoded
absolute Windows file paths, suggesting the target production log destination is local disk,
not a centralized/aggregated log store.

### What is logged
- **Every WCF operation entry** logs a message including the operation name and often the
  `sessionKey`/key business identifiers in plain text, e.g. `"getData started | SessionKey:
  {request.sessionKey}"` (`FormSessionService.svc.cs` line 158), `"saveContainer call with
  request completed:...and session id: ...and resume path:..."` (line 303).
- **Verbose step-by-step tracing** inside the dynamic concept-mapping pipeline (`getData`) —
  e.g. `"Dynamic mode enabled."`, `"ConceptList retrieved from cache | Count: ..."`, `"Fetching
  ProcedureList from DB..."` (`FormSessionService.svc.cs` lines 174-279) — useful for
  debugging but not gated by a log level (always executes, even in production).
- **Per-stored-procedure execution tracing** with timing, in `DAL/DBMessages.cs`
  `ExecuteHisoProcedure`/`GetProcedureList` (`ConceptMapper/HisoConceptDetail.cs`
  `GetProcedureList`) — includes procedure name, thread ID, and elapsed milliseconds via
  `Stopwatch`, which is a reasonable ad-hoc performance-log pattern.
- **Exceptions** are logged via `WriteExceptionLog(ex[, contextMessage])` in most (but not
  all) catch blocks.
- **Document/DMS operations** log GUIDs, connection strings (`"AddDirectDMS (Connection
  string): " + ...ConnectionStrings["ConectionStringPMS_NZ_DMS"].ToString()` — `Mapper.cs`
  line 866) and result codes.

### What is NOT logged / gaps
- **No correlation ID / request ID** is generated or propagated across log lines; the only
  natural correlation key is the free-text `sessionKey` value embedded inline in messages —
  fragile for log parsing/aggregation and means log lines cannot be reliably joined without
  string parsing.
- **No structured/JSON logging** — all log calls take a single free-text string (with
  ad hoc `$"..."` interpolation), making downstream log analytics harder.
- **No explicit security-event logging** — failed session lookups
  (`HealthLinkSession.GetByGUID` returning null), repeated invalid `sessionKey` attempts, or
  suspicious access patterns are not specifically logged as security events (a failed lookup
  simply results in a swallowed exception and a `null` return — see `SecurityAnalysis.md`
  finding #4).
- **Sensitive data logged in plaintext**: full connection strings (including embedded
  credentials) are written to the log in `Mapper.cs` line 866
  (`"AddDirectDMS (Connection string): " + ...ToString()`), and session/business identifiers
  (`PatientId`, `PracticeId`) appear throughout — a data-exposure risk if the log store is not
  itself access-controlled.
- **Static mode `getData` branch has no logging at all** (`FormSessionService.svc.cs` lines
  272-277, `#region Static` is an empty placeholder with a comment "Add logs same way as
  above" — i.e., a **known, self-documented, unfinished** logging gap).
- Not every catch block logs before rethrowing/swallowing — several silent
  `catch (Exception) { }` blocks exist with **zero logging** (e.g., `Mapper.GetAccidentInformation`).
- **Log levels are not used** — there is no visible distinction between debug/info/warn/error
  severities in the `Logger` API surface used here (both methods are unconditional writes).

### Traceability
Because there is no correlation ID and no consistent structured field for `sessionKey`
across all log lines, tracing a single clinical session's full request/response lifecycle
through the logs would require manual string-matching on the GUID text, which is workable but
fragile, especially under concurrent load (interleaved log lines from multiple sessions with
no distinguishing structured field beyond the raw sessionKey substring).

## Risks
- Logging credentials/connection strings and PHI-adjacent identifiers (`PatientId`,
  `PracticeId`) in plaintext logs is a data-protection/compliance risk for a healthcare
  system, especially if `LogRoot`/`SoapMessageLogRoot` paths are not tightly access-controlled.
- Absence of correlation IDs will make it materially harder to debug production issues at
  10,000-concurrent-user scale, where log interleaving across threads/requests will be heavy.
- The self-documented "static mode" logging gap and inconsistent exception logging mean some
  failure modes are currently invisible in production logs.

## Recommendations
- Adopt a structured logging framework (e.g., Serilog) with a per-request correlation ID
  (e.g., a `X-Correlation-Id` equivalent) propagated through all log statements in the unified
  platform.
- Never log connection strings, passwords, or other secrets; mask/redact PHI-adjacent
  identifiers or ensure the log store itself meets the same access-control bar as the
  database.
- Add explicit security-event logging (failed session/token validation, authorization
  denials) as its own category, separate from general application tracing.
- Introduce log levels (Debug/Info/Warn/Error) so verbose tracing can be turned off in
  production without losing error-level visibility.
