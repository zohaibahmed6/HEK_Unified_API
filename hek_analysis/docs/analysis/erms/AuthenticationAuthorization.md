# ERMS Web API — Authentication & Authorization

## Summary
ERMS does not use any of the OWIN/Identity/OAuth packages it references — authentication is a bespoke, database-validated GUID-token scheme scoped to a specific patient/encounter/practice context, re-checked on every request; there is no role, claim, scope, or permission model of any kind.

## Findings

### Authentication mechanism
1. Client POSTs credentials (username, password) plus `patientId`/`encounterId` to `/api/authenticate` (XML) or `/col/authenticate` (JSON).
2. IDs are Base64-decoded (APIController only) and then decrypted (`EncryptionManager.GetDecryptString`) to recover the underlying integer patient/encounter id and the embedded practice id (see BusinessRules.md BR-01/BR-02).
3. Credentials + decoded IDs + a configured expiry (`AppSettings["ExpiryInDays"]`, observed `0.5` days) are passed to `HSSDA.InsertAndValidateToken(...)`, which calls the stored procedure `[HSS].[uspInsertAndValidateToken]`.
4. On success, the stored procedure returns a `Token` (GUID string), an `Expiry` timestamp, and a `PracticeId`; these are returned to the caller as the XML `<Authentication>` or JSON `{Token, Expiry, PracticeId}` payload.
5. On every subsequent call, the caller supplies the token via the `Authorization` HTTP header (stripped of a literal `"Bearer"` prefix if present — evidence: `GetAuthorizationToken()` in both controllers, `Utility.Instance.ToString(Request.Headers.Authorization).Replace("Bearer", string.Empty).Trim()`), and the API **re-validates it against the same stored procedure** together with the (now-decoded) patient id, encounter id, and practice id for that specific call: `HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error)`.

Evidence: `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Controllers\APIController.cs` `Authenticate()` (lines 34-124) and every other action's `if (HSSDA.InsertAndValidateToken(...))` guard; `E:\NZTFS\ermsapi\DevLocal\DAL\South\HSSDA.cs` lines 928-992.

### What framework-level auth is present — and isn't
- `Startup.cs`'s OWIN `Configuration(IAppBuilder app)` method body is **empty** — no `app.UseOAuthAuthorizationServer(...)`, no `app.UseCookieAuthentication(...)`, no `app.UseWebApi(...)` even. Evidence: `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Startup.cs`.
- `Web.config` sets `<authentication mode="None"/>` — ASP.NET Forms/Windows auth is explicitly disabled; the app relies entirely on manual per-action checks.
- No `[Authorize]` attribute is used anywhere in either controller; authorization is enforced imperatively with `if (HSSDA.InsertAndValidateToken(...)) { ... } else { error = "Invalid token value!"; }` inside every action, and on failure the response is still HTTP 200 with an `<Error>`/`error` body rather than HTTP 401/403 (see SecurityAnalysis.md).
- The referenced `Microsoft.AspNet.Identity.*` and `Microsoft.Owin.Security.*` (OAuth/Cookies/Facebook/Google/MicrosoftAccount/Twitter) packages are entirely unused — no `IdentityDbContext`, no `UserManager`, no external-login provider configuration exists in the read source.

### Authorization / permission model
- There is **no role, claim, or scope model**. A valid token proves only that the caller previously authenticated for a specific `(patientId, encounterId, practiceid)` tuple; every action re-checks that same tuple, so authorization is effectively "does this token match this specific patient+encounter+practice," not "is this user allowed to perform this action."
- There is no user-identity concept surfaced to the API layer beyond the username/password submitted at authenticate time (the username is not carried forward in subsequent calls at all — only the token + patient/encounter/practice ids are).
- `COLController.SaveInvoice` (a financial write) uses the exact same token-validation guard as every read-only action — there is no elevated-privilege check for write operations vs. read operations.

### Session handling
- No server-side session object beyond what `uspInsertAndValidateToken` persists in the database (not visible in this codebase).
- No sliding expiration observed — `ExpiryInDays` appears to be set once at authenticate time; whether the token is renewed by ongoing per-request calls to `InsertAndValidateToken` (which also *inserts*, per its name) is ambiguous from the C# call sites alone.
> Assumption: `uspInsertAndValidateToken`'s "Insert" behavior on subsequent (non-authenticate) calls may or may not extend/refresh the token's expiry — this cannot be confirmed without the stored procedure source.

### Password policy
> Unable to verify from available source. Password validation happens entirely inside `[HSS].[uspInsertAndValidateToken]`; no password complexity/hashing/lockout logic exists in the C# codebase reviewed.

### CORS as a de facto authorization boundary
- `COLController` allows CORS from any origin (`origins: "*"`), meaning any website can issue authenticated `fetch`/`XHR` calls to it from a user's browser if the user's browser ever holds a valid token (e.g., in JS memory/localStorage) — this widens the practical attack surface even though the token itself is not a cookie. See SecurityAnalysis.md SEC-04.

## Evidence
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Startup.cs`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Web.config` (`<system.web><authentication mode="None"/>`)
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Controllers\APIController.cs`, `COLController.cs` (`GetAuthorizationToken()`, every action's token-check guard)
- `E:\NZTFS\ermsapi\DevLocal\DAL\South\HSSDA.cs` (`InsertAndValidateToken` overloads, lines 928-992)
- `E:\claude_projects\hek_analysis\docs\_source_docs\ERMS_doc.md` (Authentication section, lines 17-21) — doc confirms this design intent ("token... is a GUID value... passed with the headers") but does not mention the per-request re-validation-against-DB mechanism or the lack of role/claims model.

## Risks
- No role/claims model means the unified platform cannot infer *any* permission semantics from ERMS's implementation — RBAC/ABAC will need to be designed fresh in Phase 4, informed only by the business rules in BusinessRules.md.
- Failing "closed" with HTTP 200 + error body (rather than 401/403) is both a security smell (harder to detect brute-force/probing in access logs by status code alone) and an integration risk if the unified platform's clients expect standard HTTP semantics.
- The unused Identity/OAuth package surface could mislead a future engineer (or an AI coding assistant) into assuming there is framework-level auth to build on — there is not.

## Recommendations
- Do not port ERMS's per-request DB-revalidated-token model as-is; replace with the unified platform's OAuth2/OIDC (or equivalent) scheme, informed by which callers (ERMS, Pegasus/COL, possibly others) need which scopes.
- Explicitly design a role/permission model for the unified API since none exists to inherit from ERMS.
- If any part of the legacy token scheme must be bridged during a transition period, wrap it behind a proper `[Authorize]`-attribute-based filter and return standard 401/403 status codes rather than 200+error-body.
