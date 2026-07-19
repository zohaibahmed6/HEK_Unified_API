# HISO — Authentication & Authorization

**Summary:** HISO has no real authentication mechanism — every SOAP operation trusts an
opaque, non-expiring session GUID supplied by the caller, and there is no role/claims-based
authorization at all; unused JWT library references suggest token-based auth may have been
planned but was never implemented.

## Findings

### Authentication type
**None, in the conventional sense.** The WCF binding explicitly disables transport and most
message security:
```xml
<binding name="FormSessionSoapBinding" ...>
  <security mode="None">
    <transport clientCredentialType="None" proxyCredentialType="None" realm=""/>
    <message clientCredentialType="UserName" algorithmSuite="Default"/>
  </security>
</binding>
```
(Evidence: `Web.config` lines 34-40.) With `security mode="None"`, the nested `<message>`
element's `clientCredentialType="UserName"` has **no effect** — WCF does not enforce message
security when the parent mode is `None`. No `<transport>` (HTTPS/client-cert) requirement is
configured either.

The de facto "authentication" is a **session GUID lookup**:
```csharp
private HealthLinkSession GetSession(string sessionKey)
{
    HealthLinkSession objSessionKey = HealthLinkSession.GetByGUID(Guid.Parse(sessionKey));
    if (objSessionKey == null)
        throw new FaultException("Invalid Session Key");
    return objSessionKey;
}
```
(Evidence: `FormSessionService.svc.cs` lines 290-298.) `HealthLinkSession.GetByGUID` performs
a single unauthenticated SQL lookup (`Appointment.usptblHealthLinkSession_GetByGUID`) with no
expiry check, no single-use/consumption marking, and no IP/client binding visible in this
code (evidence: `Mapper.cs` lines 989-1046). Whatever session lifecycle/expiry exists must be
enforced entirely inside that stored procedure — **Unable to verify from available source**
since the procedure body is not included in this codebase.

### Authorization / role / claims model
**None found.** There is no role, claim, or permission check anywhere in the reviewed
source — once a session GUID resolves, the caller can invoke any of the 6 operations and
`processAction`'s "save"/"addTask" branches unconditionally (subject only to the
`IsDynamic` config toggle, which is a feature flag, not an authorization control).

### Session handling
- Session context (`HealthLinkSession`) is fetched fresh from the database on **every**
  operation call (no in-process session cache), which is safe from a staleness perspective
  but means the DB is the sole source of truth for session validity.
- No explicit session expiry/timeout logic exists in the C# code reviewed.
- No session invalidation/logout operation is exposed by the WCF contract.

### Token/credential handling
- `getDeliveryOptions` returns a **plaintext EDI sender password** (`ConfigurationManager.AppSettings["Password"]`) to the calling client in the SOAP response body (evidence: `FormSessionService.svc.cs` line 122, appSetting `Password` in `Web.config` line 84 — currently a placeholder value `"1"`, but the mechanism sends a password over the wire in a service response).
- Microsoft.IdentityModel.JsonWebTokens / System.IdentityModel.Tokens.Jwt (v6.5.1.0) assemblies are referenced in `Hiso.csproj` (via `bin\...dll` hint paths) but **no code in the reviewed source constructs, validates, or consumes a JWT**. This looks like either dead/vestigial references or groundwork for an authentication upgrade that was never completed — flag for the client as a `DocumentationGap`.
- `WcfConfigValidationEnabled=True` is set in the `.csproj`, but this only validates config
  well-formedness, not a security control.

## Risks
- **Critical**: any party who obtains a valid session GUID (e.g., via network interception,
  log exposure, or referral link sharing) can fully impersonate that clinical session with no
  further check — a serious risk for a healthcare system handling ACC accident-claim and
  patient clinical data.
- **Critical**: `security mode="None"` means the SOAP channel itself may run over plain HTTP
  with no transport encryption unless enforced entirely outside this application (e.g., at a
  reverse proxy/load balancer) — **Unable to verify from available source** whether TLS is
  terminated upstream in production; this must be confirmed operationally, but the
  application itself does not require it.
- No authorization model means there is no way to restrict which practices/providers can
  perform which actions beyond what the session's own context implies — a compromised or
  misused session GUID has full authority over its linked patient/provider/practice data.

## Recommendations
- For the unified platform, replace GUID-only session trust with real bearer-token
  authentication (OAuth2/OIDC or signed JWT) plus explicit authorization checks per
  operation/resource (e.g., verify the caller's practice/provider claims match the resource
  being accessed).
- Enforce TLS at the application/binding level (`security mode="Transport"` at minimum) not
  just via network topology assumptions.
- Never return credentials (EDI passwords) in a service response body; if downstream systems
  require them, deliver via a secure secret-retrieval channel instead.
- Clarify with the client whether the unused JWT library references indicate planned but
  unfinished authentication work, and capture that as input to the unified SRS.
