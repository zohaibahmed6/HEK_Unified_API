# Auth & Gateway Usage Guide

## Legacy compat authentication (byte-compatible, unchanged from each source system)

| System | Endpoint(s) | Scheme | Notes |
|---|---|---|---|
| KARO/HSS | `GET/POST /karo/authenticate` | Username/Password (+ optional PatientId/EncounterId/system/pho) JSON or query string | `HssAuthenticateRequest` → `KaroAuthenticateQuery` → real HSS token issuance. Always HTTP 200, `status` field in body carries success/failure (legacy envelope preserved). |
| ERMS | `POST /erms/authenticate` | `Credential` XML body (Username/Password/PatientId/EncounterId) | `ErmsCredentialTranslator`, XML wire shape from `ERMS_doc.md`. |
| COL/Pegasus | `POST /erms/col/authenticate` | JSON body (undocumented shape, `ColCredentialTranslator`) | Explicitly flagged as inferred — COL is undocumented per the SRS. Response is `ColUserAuthenticate` (Token/Expiry/PracticeId/lowercase `error`), always HTTP 200. |
| HISO | Session-based, not a login call | `HealthLinkSession` resolved via `[Appointment].[tblHealthLinkSession]` | `HisoSessionResolver`/`ResolveHisoSessionQueryHandler`, 12-hour expiry, structured security-event logging on miss (ADR-007 fallback-search-on-miss). |

Per ADR-003: origin scope (which legacy system a token belongs to) is always assigned
structurally by the compat endpoint itself — **never trusted from a caller-supplied field** — so
each legacy consumer's token is hardcoded to its own origin scope at mint time.

## Canonical / unified auth (JWT bearer)

`POST /auth/token` issues HEK's own resource-scoped JWT (`JwtTokenIssuer`,
`src/Infrastructure/Auth/JwtTokenIssuer.cs`):

- Signed HS256, key resolved via `ISecretProvider` (never a literal), issuer `hek-core-api`,
  audience `hek-core-api-clients`.
- Claims: `PatientId`, `PracticeId`, `OriginScope` (always), plus `EncounterId`, `PracticeCode`,
  `Environment` when present on the `ResourceScope`.
- Consumed downstream by `ResourceScopeAuthorizationHandler` and the `ResourceScoped` /
  `PlatformAdmin` / `BillingWrite` policies, and by `CanonicalDemographicsController`'s
  per-`OriginScope` field-allowlist (FR-5 field scoping — see `AllowedFieldsByOrigin` in that
  controller).
- Field scoping: a consumer may access only the fields defined in its own legacy standard (FR-5) —
  implemented today as a static per-`OriginScope` allowlist, not a general scope-configuration
  subsystem (deliberate simplification for the demo).
- Auditing: every canonical call logs consumer/timestamp/endpoint/exact-fields-returned via the
  existing correlation-ID/Serilog pipeline (FR-6).

### Open item: direct (non-legacy) callers — UNRESOLVED
`POST /auth/token` is gated to return **HTTP 501** for a direct canonical caller because no source
document defines what `OriginScope` such a caller should be assigned (ADR-012 Decision 4;
`PROJECT_STATUS.md` open item 26 — "no source document says what originScope a direct canonical
caller should get; ADR-003 says origin scope must be structural, never self-reported"). As a
demo/testing shortcut only, an explicit `OriginScope` field was added to `TokenRequest`
(`[JsonConverter(typeof(JsonStringEnumConverter))]`, applied to just this one field) so a caller can
post e.g. `"originScope": "Karo"` — this is explicitly flagged in both the contract's doc comment
and the controller as a shortcut, not a resolution of item 26. Every legacy consumer instead reaches
the platform through its own compat entry point (`/karo/authenticate`, `/erms/authenticate`,
`/erms/col/authenticate`), which are unaffected by this open item.

## Security posture (see `docs/assessment-2026-07-22.md` §3 for full detail)
JWT bearer infrastructure exists but is flag-disabled (`Auth:Enabled=false` by default); rate
limiting exists but is flag-disabled; legacy compat endpoints are unauthenticated beyond their own
legacy token schemes by design (deploy behind network segmentation).
