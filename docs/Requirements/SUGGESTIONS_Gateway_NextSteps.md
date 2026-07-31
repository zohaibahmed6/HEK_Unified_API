# Suggestions: Concrete Next Steps for the Gateway

Companion to `RESEARCH_Auth_AutoScaling.md`. Where that document explains concepts, this one recommends **one specific path per topic** — not an exhaustive options list — so it's actionable for the next sprint and demo-ready for stakeholders.

---

## 1. Fix: Entra ID Verification (highest priority — a real gap, not just a gap in research)

**Problem:** `src/Infrastructure/Auth/EntraIdIdentityValidator.cs` currently authenticates the *gateway's own app identity* with Entra ID, not the *calling application's* identity. The caller's actual credentials are never checked once `Auth:Enabled=true`.

**Recommended fix:**
1. Each calling application gets its own Entra ID App Registration (an Azure admin task per integrating system/partner — not gateway code).
2. Callers acquire their own token via the Client Credentials flow, then call our gateway with `Authorization: Bearer <token>`.
3. The gateway validates that **inbound** token against Entra ID's public signing keys (issuer `https://login.microsoftonline.com/{tenantId}/v2.0`, audience = our gateway's App ID) — conceptually a second JWT bearer scheme alongside the existing one in `Program.cs:182-193`, just pointed at Entra's metadata instead of our own signing key.
4. Our existing authorization policies (`ResourceScoped`, `PlatformAdmin`, `BillingWrite`) stay exactly as they are — they run *after* the Entra token is validated, unchanged.
5. The existing `Auth:Enabled=false` local/dev fallback is preserved — this fix doesn't break local testing before a real tenant exists.

**Effort:** Scoped, standard ASP.NET Core pattern (adding an Entra/Azure AD JWT bearer scheme is common and well-documented) — not a redesign.

**Blocker:** Full end-to-end testing needs a real Entra tenant + App Registration (see §3). The code itself can be written and unit-tested before that exists.

**Note on legacy endpoints:** this fix applies to the canonical/Entra path only. Legacy compat controllers (HISO/ERMS/KARO/COL) do not call through canonical endpoints (verified in code) and are explicitly out of scope here — see §4.

---

## 2. Enable Rate Limiting

**Recommended configuration** (uses the existing `RateLimitOptions` toggle already in code — no new middleware needed):

| Endpoint group | Limit |
|---|---|
| Login/authenticate (`/erms/authenticate`, `/auth/token`, and KARO/COL/HISO equivalents) | **10 requests/minute** per client/IP |
| All other endpoints | **30 requests/minute** per client |

**Why two tiers:** login endpoints are the brute-force/credential-stuffing target and need a tight limit; general data endpoints need a looser limit that stops abuse without disrupting normal use.

**Effort:** Configuration change against existing infrastructure (`Program.cs`, near the rate-limit registration block) — find the exact policy-registration point and add two named policies, applied respectively. Not new architecture.

---

## 3. Stand Up a Real Entra ID Tenant / App Registration

**This is an Azure admin/ops task, not a coding task**, and is the actual prerequisite blocking §1 from being fully testable and blocking `Auth:Enabled` from being safely flipped on:

1. Confirm whether the org already has an Azure/Microsoft 365 tenant (likely yes if any Microsoft services are already in use).
2. Create an App Registration for the gateway itself.
3. Supply the resulting `TenantId` / `ClientId` / `ClientSecret` via the existing secret provider.

**Timeline depends entirely on whether an Azure/Entra presence already exists** — this is the one item genuinely outside engineering's control and worth raising directly with Dr. Javad.

---

## 4. Legacy Endpoint Auth — Optional Cleanup Only, Not a Priority

Legacy compat controllers (HISO/ERMS/KARO/COL) call their own handlers directly and never route through canonical/JWT-protected endpoints — this was verified in code, not assumed. There is no functional benefit to adding JWT/claims translation here today, since there's no call chain for it to plug into.

**Recommendation: do not prioritize this.** If pursued later, purely for pattern consistency, it would still validate legacy credentials against our own local DB/session-store exactly as today (Entra ID has no role here — legacy users were never registered there) — and even then, the legacy client sees zero behavior change either way.

---

## 5. Auto-Scaling: Move to Azure Container Apps

**Recommended path:** deploy the existing Docker images (`src/Api/Dockerfile`, `frontend/Dockerfile`) to **Azure Container Apps**, not App Service or Kubernetes.

- Container-first platform — fits our existing setup directly, no rework of the Dockerfiles needed.
- Supports scale-to-zero and automatic scale-up under load — matches "no manual intervention" directly.
- Kubernetes (AKS) is unnecessary complexity at our current scale (one gateway, four backend systems) — revisit only if that changes materially.

**Open items requiring org-level decision, not engineering:**
- Which Azure region (affects latency/hosting only, not the scaling mechanism itself).
- Whether existing `docker-compose.yml` env vars/secrets need an Azure-native secrets equivalent (e.g. Key Vault) for production.

---

## 6. Anticipated Questions / Talking Points (for the live demo)

Quick-reference answers in case Dr. Javad asks:

- **"What's OAuth vs. OIDC vs. our own JWT?"** — OAuth/OIDC prove who someone is and hand over a token; our own RBAC (`PlatformAdmin`/`BillingWrite`/`ResourceScoped`, already built) decides what that identity is allowed to do — separate concerns.
- **"Why Client Credentials flow and not 'Login with Google'-style?"** — our callers are always systems/applications, never a human logging in through a browser.
- **"Is Entra ID third-party-only?"** — No, it's a protocol; Entra ID is just the trusted implementation we're using rather than building our own.
- **"Does auto-scaling solve abuse/security?"** — No, that's rate limiting's job (§2) — scaling handles legitimate load growth, not a single bad actor.
- **"Is HTTPS guaranteed everywhere?"** — Canonical/new paths, yes. Some legacy paths need verification before that claim is made — flagged honestly, not assumed.
- **"Enterprise system?"** — Yes by sensitivity/importance (patient data across multiple backends); not yet by scale/complexity (not Kubernetes-scale).

---

## Suggested Order of Work

1. Enable rate limiting (§2) — fast, low-risk, immediate security value.
2. Implement the Entra ID inbound-token-validation fix (§1) — can be written now, fully testable once §3 lands.
3. Raise the Entra tenant/App Registration ask (§3) and the Azure region decision (§5) with Dr. Javad directly — these are the two genuinely blocking, non-engineering decisions.
4. Deploy to Azure Container Apps (§5) once region is confirmed.
5. Leave legacy endpoint auth (§4) out of scope unless explicitly requested later.
