# KARO — Authentication & Authorization

**Summary:** KARO has no framework-level authentication or authorization — despite referencing the full ASP.NET Identity/OWIN OAuth package set, the OWIN `Startup` is empty, `Web.config` disables ASP.NET authentication entirely, and access control is implemented entirely by hand inside each controller action via a custom bearer-token string validated against a stored procedure.

## Findings

### Authentication type
- **Not** OAuth2/JWT/Cookie/OpenID/Azure AD, despite `packages.config` referencing `Microsoft.Owin.Security.OAuth`, `.Cookies`, `.Google`, `.Facebook`, `.Twitter`, `.MicrosoftAccount`, and `Microsoft.AspNet.Identity.*` (2.2.1). None of these are configured: `Startup.cs`'s `Configuration(IAppBuilder app)` method body is empty (`Startup.cs` lines 13-15).
- `Web.config`: `<authentication mode="None" />` (line 45), and `<modules><remove name="FormsAuthentication" /></modules>` (line 51) — ASP.NET's built-in authentication pipeline is explicitly disabled.
- **Actual mechanism:** a custom opaque token (GUID-like, e.g. doc sample `"5895CBDF-AB10-4A22-99F8-DC26E372104B"`) is:
  1. Issued by `POST /api/Authenticate` (or the undocumented `GET /api/Authenticate`) after validating `username`/`password` server-side via `[HSS].[uspInsertAndValidateToken]` (`Controllers\APIController.cs` lines 63, 133; `DAL\South\HSSDA.cs` lines 795-841).
  2. Returned to the client along with an `expiry` timestamp and a `practiceId`.
  3. Sent back by the client on every subsequent call in the standard `Authorization: Bearer <token>` HTTP header (per `KARO_HSS_doc.md` and confirmed in code: `GetAuthorizationToken()` strips the literal string `"Bearer"` from `Request.Headers.Authorization`, `Controllers\APIController.cs` lines 1976-1987).
  4. Re-validated **per call**, scoped to the specific `patientId`+`encounterId`+`practiceId`(+`pho`) via `HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error)` (same overload reused in every action, e.g. `Controllers\APIController.cs` line 197, 252, 307...).

### Roles / claims / permission model
No role or claims model was found anywhere in the code. There is a single binary authorization outcome per request: "is this token valid for this patient+encounter+practice combination, yes/no" — returned as a boolean from `HSSDA.InsertAndValidateToken` (`DAL\South\HSSDA.cs` lines 843-859). There is no user-role distinction (e.g., doctor vs. nurse vs. admin) visible in the API layer; any such distinction, if it exists, is entirely inside the stored procedure and/or the calling application's own logic.
> Unable to verify from available source whether the underlying `[HSS].[uspInsertAndValidateToken]` procedure encodes any role/permission logic — its body is not present in this repository.

### Session handling
- Sessions are stateless from the API's perspective — no `Session` object usage found; the "session" is entirely represented by the opaque token stored server-side (in the PMS database, presumably a token table) and validated per call.
- Token expiry is returned to the client (`Expiry` field) but the API itself does not appear to reject calls based on client-side expiry checking — expiry enforcement, if any, happens inside the stored procedure (`Controllers\APIController.cs` lines 68-77 only *parses* the returned expiry to build the response; it does not independently compare it to `DateTime.Now`).

### Token/refresh strategy
- No refresh-token endpoint or mechanism was found — `Authenticate` must be called again with username/password to obtain a new token. There is no distinction between an access token and a refresh token.
- Token expiry duration is configurable per call via an optional `expiryInDays` parameter on one `HSSDA.InsertAndValidateToken` overload (`DAL\South\HSSDA.cs` lines 807-808, 829-830) but this parameter is **never passed a non-zero value from `APIController.cs`** — every controller call goes through the overload that defaults `expiryInDays` to `0`, meaning the API layer never explicitly requests an expiry; it is left entirely to the stored procedure's default.

### Password policy
No password policy, complexity rule, or hashing/salting logic was found in the KARO codebase — the `username`/`password` values are passed as plain `SqlParameter`s directly into `[HSS].[uspInsertAndValidateToken]` (`DAL\South\HSSDA.cs` lines 817-820).
> Unable to verify from available source whether password hashing/policy is enforced inside the stored procedure or in the PMS system that owns the credential store; no evidence either way exists in this repository.

### Credential handling issues (cross-referenced in SecurityAnalysis.md)
- `GET /api/Authenticate` accepts `username`/`password` as URL query-string parameters (`Controllers\APIController.cs` line 31) — these are trivially captured in web server access logs, browser history, and proxy logs.
- Both `Authenticate` overloads log the raw plaintext password to the application's flat-file logger (`Controllers\APIController.cs` lines 62, 132: `WriteLog("In Authenticate: PatientId/EncounterId/practiceid/Username/Password/system/pho >> " + ... + "/" + password + "/" + ...)`).

## Evidence
See file/line citations inline above.

## Risks
- The presence of unused Identity/OAuth packages is likely to mislead a future engineer (or an automated dependency scanner) into believing modern authentication is in place when it is not.
- Per-call re-validation of the token against a specific patient/encounter combination is a reasonable design intent (fine-grained authorization) but its correctness is entirely dependent on an opaque stored procedure that could not be reviewed in this analysis.
- No token revocation/logout endpoint was found — a compromised token can only become invalid by expiry (whose default duration is also unknown from this codebase).

## Recommendations
- In the unified platform, replace this pattern with a standard, auditable token scheme (e.g., short-lived JWT with explicit expiry claims validated server-side without a DB round-trip, or a properly configured OAuth2 client-credentials/resource-owner flow) and eliminate the GET-based credential submission entirely.
- Obtain and review `[HSS].[uspInsertAndValidateToken]`'s definition to understand exactly what "valid for this patient/encounter" means today, since this is the sole authorization boundary protecting all patient data in this API.
- Decide, with the client, whether any role/permission differentiation is actually needed for the unified platform — today's binary valid/invalid model may be insufficient for the target 10,000-concurrent-user, multi-location deployment.
