# HISO — Security Analysis (OWASP-style review)

**Summary:** HISO's most severe security issues are architectural (no real authentication,
`security mode="None"`, fault-detail leakage, and thread-unsafe shared DAL state) rather than
classic SQL injection, since all observed database calls use parameterized stored
procedures; document-generation and hardcoded-secret issues are also significant.

## Findings

### 1. Broken Authentication (Critical)
**Severity:** Critical
**Evidence:** `Web.config` lines 34-40 (`security mode="None"`); `FormSessionService.svc.cs`
`GetSession`/`HealthLinkSession.GetByGUID` (`Mapper.cs` lines 989-1046) — session validity is
solely "does this GUID exist in the DB", with no expiry, single-use, or client-binding check
visible in the application layer.
**Recommendation:** Require transport security (TLS) at the binding level, replace GUID
lookup with a real, time-bound, signed session/bearer token, and add session
expiry/invalidation.

### 2. Hardcoded/plaintext secrets (High)
**Severity:** High
**Evidence:**
- `Web.config` lines 67-72 and 74-113: four SQL Server connection strings with plaintext
  `User ID=pms_nz;Password=pms@@nz` (credential present, redacted here) reused across all
  four databases.
- `DAL/DALHelper.cs` line 17 and `DAL/DALHelperParameterCache.cs` line 18:
  `private static string mstrEncryptionCommand = "OPEN SYMMETRIC KEY DBDX DECRYPTION BY PASSWORD = 'tcpepms*1'"` — a hardcoded database symmetric-key decryption password committed to source control.
- `Web.config` line 82-84: `UserID`/`Password` EDI credentials in plaintext appSettings.
**Recommendation:** Move all credentials/keys to a secrets manager (Azure Key Vault, AWS
Secrets Manager, etc.) or environment-injected configuration; rotate the exposed
`tcpepms*1` symmetric key password immediately since it is in source history.

### 3. Verbose error / fault-detail exposure (Medium-High)
**Severity:** High for a system handling clinical/health data
**Evidence:** `Web.config` line 26: `<serviceDebug includeExceptionDetailInFaults="true"/>`
combined with every WCF operation doing `catch (Exception ex) { ...; throw new
FaultException(ex.Message); }` (e.g. `FormSessionService.svc.cs` throughout) — internal
exception messages (potentially including SQL error text, file paths, or stack details) are
returned to any SOAP caller.
**Recommendation:** Disable `includeExceptionDetailInFaults` in production; return generic
fault codes/messages and log full detail server-side only.

### 4. Improper exception handling / stack-trace loss and silent failure (Medium)
**Severity:** Medium
**Evidence:** Pervasive `catch (Exception ex) { throw ex; }` pattern (destroys original stack
trace — e.g. `DAL/DbAccess.cs` throughout, `Task.cs`, most Builder classes) and, more
seriously, silent-swallow patterns that return null/default without logging or rethrowing,
e.g. `Mapper.cs` `HealthLinkSession.GetByGUID` (`catch (Exception) { }`) and
`Mapper.GetAccidentInformation` (`catch (Exception) { }`) — a database outage or malformed
GUID looks identical to "session not found" to the caller, and a DB failure while loading
accident info is silently swallowed and returns `null` instead of surfacing an error.
**Recommendation:** Use `throw;` to preserve stack traces; never silently swallow exceptions
in security-relevant paths (session resolution); log and propagate distinguishable error
codes.

### 5. Thread-unsafe shared database access layer (High — availability/data-integrity risk)
**Severity:** High, directly relevant to the 10,000-concurrent-user target
**Evidence:** `DAL/DbAccess.cs` declares `private static SqlConnection DbConn`, `private
static SqlDataAdapter DbAdapter`, `private static SqlCommand DbCommand`, `public static
SqlTransaction DbTran`, and `public static DataSet ds` — **one shared connection/command per
AppDomain**, used across all concurrent requests that go through this class (`selectQuery`,
`executeQuery`, `executeStoredProcedure`, `beginTrans`/`commitTrans`/`rollbackTrans`, etc.).
Concurrent calls will race on `DbCommand.Parameters.Clear()`/`Add()` and on the shared
transaction, risking cross-request data corruption or query mix-up under load.
**Recommendation:** Eliminate static/shared ADO.NET objects; use connection-per-call (or
async connection pooling) exclusively — this is not optional for a 10,000-concurrent-user
target.

### 6. Unsafe/uncontrolled document rendering (Medium-High)
**Severity:** Medium-High
**Evidence:** `ConceptMapper/TypeConverter.cs`, `ConvertHTMLToByte` loads arbitrary
byte content as HTML via `Aspose.Words.Document(stream)` with `LoadFormat.Unknown` and
renders it to PDF; the HTML content originates from clinical attachment data stored in the
PMS/DMS (ultimately traceable back to previously-submitted form/document content). Aspose.Words'
HTML importer can resolve external resource references (images, stylesheets) embedded in the
HTML unless explicitly disabled, which is a potential **SSRF vector** if attacker-controlled
HTML ever reaches this path. **Unable to verify from available source** whether Aspose's
resource-loading callback is restricted anywhere in this codebase (no `ResourceLoadingCallback` or similar override was found).
**Recommendation:** Sanitize/allow-list HTML before conversion, and explicitly disable
external resource fetching in Aspose (`IResourceLoadingCallback` returning `Default`/`Skip`
for remote URIs) before any rewrite reuses this pattern.

### 7. In-memory injection-adjacent code smell via `DataTable.Select()` (Low)
**Severity:** Low (not a direct SQL injection since `DataTable.Select` is an in-memory
`RowFilter` expression evaluator, not SQL against the database) but still a real robustness
bug.
**Evidence:** Repeated pattern across `Acc45Builder.cs` line 57, `PatientBuilder.cs` line 60,
`PatientEmployerOrganisationBuilder.cs` line 60, `Acc45DiagnosisBuilder.cs` line 66,
`Acc45ReferralBuilder.cs` line 57: `dtMapping.Select("ConceptName = '" + item.Key + "' OR
Description = '" + item.Key + "'")` — if a clinical form field's `conceptName`/`name`
attribute (which ultimately originates from the form-engine XML, i.e., external input)
contains a single quote or other `RowFilter`-syntax character, the expression breaks or
matches unintended rows.
**Recommendation:** Use parameterized `DataTable.Select` alternatives (LINQ `Where` against
in-memory collections) instead of string-built filter expressions.

### 8. No confirmed SQL injection in database queries (Informational — positive finding)
**Severity:** N/A (documented as a control that is working)
**Evidence:** Every `SqlCommand`/`DALHelper` invocation found in the reviewed source uses
`CommandType.StoredProcedure` with `SqlParameter` objects for all externally-derived values;
`DAL/DbAccess.cs` does expose free-text SQL execution methods (`selectQuery`, `executeQuery`,
etc.) but no call site in this codebase was found passing concatenated user input into them
(see `DatabaseAnalysis.md`).
**Recommendation:** Confirm no callers outside this reviewed source (e.g., in `MHNEntity` or
other PMS modules) misuse `DbAccess`'s free-text methods with unsanitized input; consider
removing those methods entirely in the rewrite since they are unused within this project.

### 9. Directory browsing enabled (Low, dev-config risk)
**Severity:** Low, but should not ship to production
**Evidence:** `Web.config` line 61: `<directoryBrowse enabled="true"/>` — the same comment in
the file explicitly warns "Set to false before deployment to avoid disclosing web app folder
information," indicating this may be a known but unresolved dev-config leak into what could
be a shared config file.
**Recommendation:** Confirm `Web.Release.config` (or deployment transform) disables directory
browsing; verify no production deployment ships with this setting.

### 10. Sensitive data returned in service responses (Medium)
**Severity:** Medium
**Evidence:** `getDeliveryOptions` returns `senderPassword` (EDI submission password) in the
plain SOAP response body (`FormSessionService.svc.cs` line 122) — sent over a channel with
`security mode="None"`.
**Recommendation:** Do not transmit credentials in response payloads; if the downstream
system needs them, use a secure out-of-band credential exchange.

## Risks (summary)
The largest security exposure for HISO is architectural — a healthcare data service with
effectively no authentication/authorization and exception-detail leakage — rather than
traditional injection flaws (which are well-mitigated here via parameterized stored
procedures). Combined with hardcoded secrets and thread-unsafe shared connections, this
service should not be exposed to untrusted networks as-is, and none of these gaps should be
carried forward into the unified platform.

## Recommendations (summary)
1. Add real authentication/authorization (see `AuthenticationAuthorization.md`).
2. Remove all hardcoded secrets; rotate the exposed symmetric key password.
3. Disable fault-detail leakage and directory browsing in production.
4. Eliminate shared/static ADO.NET state.
5. Harden Aspose-based document conversion against SSRF/resource injection.
6. Replace ad-hoc `DataTable.Select` string building with safe LINQ filters.
