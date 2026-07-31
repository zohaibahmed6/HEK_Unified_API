# Daily Work Summary — July 31, 2026

**Scope:** Started from a question about whether technical logs were complete, expanded into a full
observability overhaul, then a systematic close-out of the remaining crosscheck gaps between the new
API and the 4 real legacy systems (HISO/ERMS/KARO/COL).

## 1. Logging Observability Overhaul

Found and fixed a real gap: several HISO Infrastructure-layer classes' errors only ever reached
`docker logs`, never the per-system log files, because those log calls didn't carry the `"System"`
tag Serilog's per-system file router matches on.

Built a proper fix, not a patch:
- **New `RequestResponseLoggingMiddleware`** — tags every request with an ambient `System` property
  (so *any* log line written anywhere during that request routes correctly, even ones that never
  explicitly tag themselves) and logs the full raw request/response body for every call.
- **`LegacyDbExecutor`** (the shared SQL helper ~30 repositories use) now logs every stored-procedure
  call centrally — procedure name, every parameter's value, and the result (rows/columns or the
  exception) — with no risk of a caller forgetting to log it.
- Fixed the 7 originally-flagged untagged log calls directly too, as a second layer of defence.

Verified live across all 4 systems (KARO/ERMS/HISO-SOAP/COL) with real test calls, tracing a single
request end-to-end via its `CorrelationId` across the request/response line, the SQL-call line, and
the readable-log summary line.

## 2. HISO `getDeliveryOptions` Config Gap Fixed

Its 4 config keys (`Hiso:PracticeEdi`/`UserId`/`Password`/`Url`) existed only in the gitignored local
dev file, never in the file Docker/Azure builds actually use — so this endpoint always returned empty
credentials in any real deployment. Added the keys with `CHANGE_ME_*` placeholders (no real value
exists to copy in — even local dev never had one). Caught and fixed a self-inflicted follow-on bug:
an unconditional Docker Compose env-var mapping was silently overriding the placeholder with an empty
string; removed it, documented exactly how to wire in real credentials once available.

## 3. Cosmetic Crosscheck Mismatches — All Resolved

Systematically closed out every remaining item from the crosscheck (11 mismatches down to 3, all 3
being genuine legacy-side behavior, not new-API defects):

- **HISO SOAP wrapper naming** (3 operations) — real legacy uses a bare `<return>` element; fixed via
  explicit message contracts.
- **KARO Authenticate extra field** — response carried an unwanted `"message":null`; removed.
- **KARO `GetDemographics` missing field + null/empty convention** — `endEnrolmentDate` was dropped
  entirely; and confirmed directly against the real legacy server that empty text fields should be
  `""`, not `null` (traced to a real .NET behavior difference: `Convert.ChangeType` doesn't throw for
  DBNull-to-string, only for DBNull-to-value-type). Fixing the shared mapper closed this gap for every
  KARO GET endpoint at once, not just the one flagged.
- **Line-ending mismatch** (ERMS/KARO free-text content) — root cause was `Environment.NewLine`
  resolving differently on Linux (this app's Docker container) vs. Windows (where real legacy runs) —
  a genuine cross-platform bug, fixed with a literal `\r\n`.

Every fix was verified by calling the real legacy servers directly (found their actual local IIS
Express ports and confirmed they're still running) rather than assumed correct from code alone.

## 4. COL Authenticate + 4 Dependent Operations Confirmed

Using real production credentials Zohaib supplied (used only in live tests, never written to any
file), confirmed the new API's COL Authenticate and its 4 dependent read operations
(`GetCurrentPatientData`/`GetProviderData`/`GetSurgeryData`/`GetDiagnosisData`) all work correctly
with real data. Discovered why real legacy could never be used for this comparison before: it
genuinely hangs (4+ minutes) on these calls in this dev environment, because this account's
connection string resolves to a remote IP address unreachable from this machine — the same class of
environment limitation found earlier with HISO's external document service, not a new-API defect.

## 5. COL SaveInvoice — Root Cause Found, Not Yet Closed

Derived the exact real request shape from legacy source and confirmed the new API's model already
matches it field-for-field, which eliminated an earlier shape-validation error. Traced the remaining
failure directly at the database level (calling the real stored procedure via `sqlcmd`, bypassing
both APIs): a `NOT NULL` column (`InsertedBy`) on the underlying table has no value supplied by
either the real legacy code or the new API's port - both use an identical call signature that simply
doesn't carry this value. This is a genuine data/environment gap (this practice has no prior COL
invoice history), not a code defect. Left open pending clarification from Zohaib on where the correct
value should come from.

## Verification

- Full API build: **0 errors, 0 warnings** after every change.
- Every fix in sections 1-4 was redeployed to Docker and confirmed with a live call against the
  running container, not just a local build check.
- Crosscheck scoreboard moved from 37 matched/11 mismatch/5 needs-payload to **49 matched/4
  mismatch/0 needs-payload** (`crosscheck/SUMMARY.md`, `dashboard.html`).

## Not Done Today / Open Items

- COL SaveInvoice's `InsertedBy` value source — needs Zohaib's input before this can close.
- Real HealthLink delivery-account credentials for HISO `getDeliveryOptions` (`senderPassword`/`URL`)
  — placeholder only, wire in via `.env`/`docker-compose.yml` once available (documented exactly how).
- Full continuation context for tomorrow is in `hek_analysis/SESSION_HANDOFF_2026-07-31.md`.
