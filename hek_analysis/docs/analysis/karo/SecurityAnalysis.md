# KARO — Security Analysis (OWASP Top 10 Review)

**Summary:** KARO has no framework-level auth, wide-open CORS, hardcoded cryptographic secrets, plaintext PHI/credential logging, and confirmed SQL-injection vulnerabilities in DAL code that ships in the same compiled library KARO depends on — this is a high-severity finding set for a system handling patient health information.

## Findings

### 1. Injection (A03:2021) — SQL Injection
**Severity: Critical**
Confirmed string-concatenated SQL in DAL classes compiled into the same `DAL.dll` that `HSSWebAPI` references:
- `DAL\DMS\DMSDA.cs` line 27: `cmd.CommandText = "Update Prompt.tblInboxFolderItem set DMSID='" + Guid + "' where InboxFolderItemID=" + InboxFolderItemID + "";`
- `DAL\MHNHL7\DBMessages.cs` line 797: `cmd.CommandText = "SELECT * FROM Config.tblScreeningTemplate where ScreeningTypeID in (" + templateIDs + ")";`
- `DAL\MHNHL7\DBMessages.cs` line 827: `cmd.CommandText = " SELECT * FROM Appointment.tblScreening where ScreeningTypeID=" + Convert.ToString(row["ScreeningTypeID"]) + " and isnull(IsDeleted,0)=0 and IsActive=1 " + " and ScreaningID not in (SELECT ScreaningID from Appointment.tblScreeningDetail) ";`
- `DAL\MHNHL7\DBMessages.cs` lines 860-864: `cmd.CommandText = "INSERT INTO [Appointment].[tblScreeningDetail](...) VALUES ('" + row["ScreaningID"] + "','" + row["PracticeID"] + "','" + jsonString + "','" + null + "')";` — note `jsonString` is inserted directly with no escaping/parameterization.
- `Evidence that KARO's own controller does NOT call these`: grep confirms `Controllers\APIController.cs` only calls `HSSDA.*` (South module); `DMSDA`/`DBMessages` are unreferenced from the controller (one commented-out line only, `Controllers\APIController.cs` line 1342). **Risk is deployment-scoped, not request-scoped, for KARO today** — but since it's the same `DAL.dll`, any future KARO change (or any sibling app sharing this DLL) that begins calling these methods inherits the vulnerability immediately.
- **Recommendation:** Parameterize all four sites before this DAL is reused anywhere in the unified platform; do not port string-concatenated SQL forward under any circumstance.

By contrast, KARO's own reachable path (`DAL\South\HSSDA.cs`) consistently uses parameterized `SqlParameter` objects with `CommandType.StoredProcedure` (e.g. lines 126-134) — **no SQL injection was found in the code path KARO's controller actually exercises.**

### 2. Broken Access Control (A01:2021)
**Severity: High**
- No framework-level `[Authorize]`; access control is manual per-action code (`Controllers\APIController.cs`, `HSSDA.InsertAndValidateToken` calls). One endpoint, `SaveScreeningCode`, performs **no token validation at all** (`Controllers\APIController.cs` lines 1924-1947) — effectively unauthenticated, though it is currently a no-op that persists nothing.
- CORS is wide open: `[EnableCors(origins: "*", headers: "*", methods: "*")]` at the controller level (`Controllers\APIController.cs` line 22) — any origin can make credentialed cross-origin requests to this API from a browser.
- **Recommendation:** Remove the wildcard CORS policy and scope it to known HSS-portal origins; ensure every action (including any future stub endpoints) enforces token validation before proceeding.

### 3. Cryptographic Failures (A02:2021)
**Severity: Critical**
- Hardcoded Rijndael (AES-family) symmetric key used to "encrypt" patient/encounter IDs: `Models\EncryptionManager.cs` line 47 — `myRijndael.Key = new byte[32] { 137, 10, 237, 69, 157, 169, 181, 70, 216, 110, 47, 209, 193, 153, 196, 109, 25, 146, 165, 140, 128, 7, 175, 122, 34, 247, 157, 143, 54, 233, 124, 219 };` — anyone with source access (or a decompiled binary) can decrypt every patient/encounter ID ever transmitted through this API.
- Hardcoded SQL Server symmetric-key decryption **password** in `DAL\HelperClasses\DALHelper.cs` line 17: `private static string mstrEncryptionCommand = "OPEN SYMMETRIC KEY DBDX DECRYPTION BY PASSWORD = 'tcpepms*1'";`
- Plaintext database credentials committed directly in `Web.config` (credential present, redacted here) across 6 environment variants (lines 15-30) — e.g., production connection string embeds `Password=pms@@nz;...User ID=pms_nz` in cleartext XML.
- **Recommendation:** Move all secrets to a managed secret store (Azure Key Vault, AWS Secrets Manager, or equivalent) before any redeploy; rotate the exposed DB credentials and the Rijndael key as part of the migration (note: rotating the Rijndael key breaks backward compatibility with any already-issued encrypted IDs — plan a transition).

### 4. Sensitive Data Exposure / Logging (A09:2021 — Security Logging & Monitoring Failures, and A02 overlap)
**Severity: High**
- Plaintext credentials logged: `Controllers\APIController.cs` lines 62, 132 log the raw `username`/`password` on every authentication attempt.
- Full clinical content (subjective/objective notes, diagnoses, assessments) logged to flat files: e.g. `Controllers\APIController.cs` lines 876-877 (`SaveClinicalNotes`) log `AppointmentAdvice`, `Assessment`, `ObjectiveNotes`, `SubjectiveNotes`, `Plans` in full.
- Log files are plain `.txt` under `Logs\` with no evidence of access control, encryption at rest, or automatic purging/retention policy (`Logger\Logging.cs`) — a filesystem-level compromise of the web server exposes PHI and credentials in cleartext, unbounded historically (no rotation size cap found).
- **Recommendation:** Redact credentials and PHI from log messages; if audit-level clinical logging is genuinely required, route it through a proper audit-log subsystem with access controls and retention policy, not the general application event log.

### 5. Cross-Site Request Forgery (CSRF)
**Severity: Low (for this API shape)**
This is a token-header-authenticated JSON API (not cookie/session-authenticated), so classic CSRF (which relies on ambient browser credentials like cookies) is largely mitigated by the bearer-token design itself — **provided** the wide-open CORS policy (finding #2) doesn't allow a malicious page to read a token from local storage and replay it, which is a separate concern (XSS-adjacent, not CSRF). No explicit anti-CSRF token mechanism was found, which is consistent with (and appropriate for) a bearer-token API, but worth noting as absent.

### 6. Missing Input Validation
**Severity: Medium**
- No model validation attributes (`[Required]`, `[StringLength]`, etc.) or `ModelState.IsValid` checks found on any DTO in `Models\APIModels.cs` or any controller action — all deserialized JSON is trusted implicitly and passed straight into stored procedure parameters (mitigated somewhat by parameterization, but not by type/range/length checks).
- `encounterId` parsing (`Split('_')`/`Split("__")`) has no bounds checking beyond `.Length` comparisons and will silently produce wrong `practiceid` values or throw unhandled exceptions on malformed input (e.g., `Convert.ToInt32(splitEncounter[1])` in `GetDocuments`, `Controllers\APIController.cs` line 372, will throw `FormatException` if the segment isn't numeric — caught generically and surfaced as a bland "unable to process" style error).
- **Recommendation:** Add explicit request validation (data annotations or FluentValidation in the rewrite) rather than relying on implicit type coercion and try/catch-as-control-flow.

### 7. Unsafe File Upload / Content Handling
**Severity: Medium**
- `SaveDocument` accepts arbitrary `byte[] MessageData` (base64-decoded by the JSON deserializer) with a client-supplied `ContentType` string used only to select a *document type ID* for classification (`Controllers\APIController.cs` `SaveToDMS`/`DocumentTypeID`, lines 1948-2001) — there is **no file-size limit, no content/magic-byte validation, and no malware/AV scanning** visible in the code path. Any binary content, mislabeled as any content type, will be accepted and stored.
- **Recommendation:** Add server-side file-size caps, content-type/magic-byte verification, and (ideally) AV scanning before persisting uploaded documents in the unified platform.

### 8. Insecure Deserialization
**Severity: Low**
JSON deserialization uses `Newtonsoft.Json.JsonConvert.DeserializeObject<T>()` against known, fixed DTO types (`Controllers\APIController.cs`, e.g. `JsonConvert.DeserializeObject<Credential>(result)`) — no `TypeNameHandling.Auto`/polymorphic deserialization was found, which is the primary Newtonsoft.Json RCE vector. Low risk as implemented.

### 9. Rate Limiting
**Severity: Medium**
No rate limiting, throttling, or brute-force protection was found anywhere (no `System.Web.Http.Owin` throttling middleware configured, `Startup.cs` is empty). The `Authenticate` endpoint (both GET and POST) is exposed to unlimited credential-guessing attempts.
- **Recommendation:** Add rate limiting / account lockout on the authentication endpoint at minimum in the unified platform; this is table-stakes for a target of 10,000 concurrent users and multiple public-facing entry points.

### 10. Security Misconfiguration
**Severity: Medium**
- `<compilation debug="true" targetFramework="4.8"/>` left enabled in the base `Web.config` (line 46) — should be `false` in production; debug mode increases information disclosure risk via verbose error pages.
- All HTTP responses return `200 OK` regardless of outcome (`Controllers\APIController.cs` `SetToJson()`, lines 2010-2019) — while not itself a vulnerability, it undermines standard security tooling (WAFs, API gateways, SIEM rules) that key off HTTP status codes to detect abuse/failure patterns.

## Evidence
All citations inline above; primary files: `Controllers\APIController.cs`, `Models\EncryptionManager.cs`, `DAL\HelperClasses\DALHelper.cs`, `DAL\DMS\DMSDA.cs`, `DAL\MHNHL7\DBMessages.cs`, `Web.config`, `Logger\Logging.cs`.

## Risks
Ranked by severity: (1) Critical — hardcoded encryption key/DB password/plaintext connection strings; (2) Critical — SQL injection present in the shared DAL library (dormant for KARO today, live risk for anything sharing `DAL.dll`); (3) High — no framework auth, wide-open CORS, PHI/credentials in logs; (4) Medium — no rate limiting, no file-upload validation, no input validation, debug mode enabled in base config.

## Recommendations
1. Treat secret rotation (encryption key, DB credentials, symmetric-key password) as a pre-migration security remediation item, independent of the platform rewrite timeline.
2. Do not port the SQL-injectable DAL methods into the unified platform under any circumstances; if their functionality (HL7 inbox update, screening template lookups) is needed, reimplement with parameterized queries.
3. Design the unified platform's auth layer from scratch using modern, standard patterns (JWT/OAuth2 with proper validation middleware) rather than adapting KARO's per-call manual token check.
4. Add rate limiting, structured input validation, and file-upload safeguards as baseline requirements for the unified platform given the 10,000-concurrent-user target.
