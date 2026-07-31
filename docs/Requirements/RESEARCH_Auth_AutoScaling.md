# Research: Modern Authentication & Auto-Scaling

**For:** Dr. Javad stakeholder review
**Prepared by:** Zohaib (AI-assisted research)
**Source ask:** "Research modern OAuth" + "Research auto-scaling for the .NET Core gateway" (see `requirement_document.md`)

This document is written in plain language on purpose — every technical term gets a one-line explanation. It's meant to be liftable almost directly into slides.

---

## Part 1 — Modern Authentication

### 1.1 What does "modern OAuth" actually mean?

**OAuth** = a standard way for an app to prove who's calling it, without ever handling that caller's raw password.

**Simple analogy:** A hotel doesn't hand you the master key. Reception gives you a **card-key** that opens only your room, for a limited number of days. The card-key is the **token**. Reception is the **trusted verifier**.

**OpenID Connect (OIDC)** = a thin extra layer on top of OAuth that answers "**who** is this" (identity), while plain OAuth only answers "**what can they do**" (access). The two are usually used together.

**OAuth has several "flows" for different situations:**

| Flow | Used when | Relevant to us? |
|---|---|---|
| Authorization Code | A human logs in through a browser (e.g. "Login with Google") | No — our callers are systems, not humans |
| **Client Credentials** | **One system calls another, no human involved** | **Yes — this is our exact case** |
| Device Code | Logging in on a TV/smart device | No |
| Refresh Token | Getting a new token without re-entering credentials | Yes, as a helper once tokens are in use |

**Bottom line for us:** our gateway is always called by other applications/systems, never by a human typing a password into a browser. So **Client Credentials** is the flow that matters — not the "Login with Google" flow most people picture when they hear OAuth.

### 1.2 What is Entra ID?

**Entra ID** (formerly Azure Active Directory) is Microsoft's identity verifier — the "reception desk" that confirms who someone is and issues them a token. Companies use it to manage employee/system identities in one place instead of everyone building their own login system.

**Important distinction — authentication vs. authorization:**
- **Authentication** (Entra ID's job) = confirming *who* someone is.
- **Authorization** (our job, always) = deciding *what* they're allowed to do.

Even with Entra ID fully adopted, our gateway keeps its own permission records. A user or system verified by Entra ID still gets **denied** if they're not in our own allow-list. Entra ID doesn't hand over access control — it only proves identity. This is true regardless of which auth mechanism issues the token; **RBAC (role-based permissions) is a separate concern from OAuth/OIDC**, and we already have the beginnings of our own RBAC today (`PlatformAdmin`, `BillingWrite`, `ResourceScoped` policies in `Program.cs`).

**OIDC/Entra ID are not inherently "third-party"** — OIDC is just a protocol; anyone (including us) could implement it. It's conventionally delegated to a trusted provider like Microsoft because building and securing your own identity system is hard and risky, not because the protocol requires it.

### 1.3 Where do we actually stand today? (verified against the code, not assumptions)

- **Canonical endpoints already use JWT bearer authentication** (`Program.cs:182-193`) — a custom, gateway-issued/verified token. This is not OAuth itself, but the same *shape* of security (bearer tokens, expiry, claims).
- **Entra ID integration is already built in code** — `src/Infrastructure/Auth/EntraIdIdentityValidator.cs` uses Microsoft's own library (MSAL) and pulls tenant/client secrets through our secret provider (never hardcoded).
- **It's currently switched off** (`Auth:Enabled=false` by default, per `docker-compose.yml`'s `AUTH_ENABLED`). While off, the code explicitly runs a documented fallback ("legacy-equivalent mode": any non-empty credential is accepted) — this is a deliberate, documented decision (ADR-008), not an oversight, because no real Entra tenant/App Registration exists yet.

**Headline point for the presentation: Entra ID support is not greenfield work — it's already built. What's missing is (a) a real Azure tenant/App Registration (an ops task, not code) and (b) a known implementation gap described below.**

### 1.4 A real gap found in the existing Entra ID code

On closer inspection, `EntraIdIdentityValidator.cs` has a flow mismatch:

- It currently acquires a token *for our own gateway app* (calling Microsoft to prove **we** are who we say we are).
- It does **not** validate a token that a *calling application* presents to us.
- Practically: once switched on as-is, almost any credential would pass, because the caller's actual identity is never checked — only the gateway's own app credentials are.

This is analogous to a security guard checking his own ID card instead of the visitor's.

**Why this happened:** passing a raw username/password into an API for it to relay to an identity provider is an older, discouraged pattern ("ROPC"). Since our callers are always systems (confirmed — see §1.1), the correct fix is the standard **app-to-app** pattern:

1. Each calling application registers itself in Entra ID (its own App Registration).
2. The calling application gets its own token from Entra ID using its own credentials.
3. It sends that token to our gateway (`Authorization: Bearer <token>`).
4. **Our gateway validates the incoming token** against Entra ID's public keys — it does not mint a token for itself.
5. Our own authorization policies still apply after that (§1.2).

This is a scoped, well-understood ASP.NET Core change — not a redesign. See `SUGGESTIONS_Gateway_NextSteps.md` for the concrete fix plan.

### 1.5 Legacy endpoints (HISO, ERMS, KARO/HSS, COL) — the hard constraint

**Requirement: zero client-visible change.** Legacy systems keep sending exactly what they send today — HISO's `SessionKey`, ERMS/KARO's username/password + patientId/encounterId. We cannot ask any existing client to change its integration.

**Verified in code:** legacy compat controllers (`ErmsCompatController`, `KaroCompatController`, `ColCompatController`, `HisoCompatController`) call their own dedicated handlers directly — they do **not** route through the canonical/JWT-protected endpoints. These are two separate paths today.

**What this means honestly:** applying JWT/claims translation to legacy endpoints would not "connect" them to anything — there's no functional call chain to plug into. The only real reason to do it would be applying the same `[Authorize]`-style policy pattern locally, purely for consistency across the codebase. Since legacy controllers don't currently have that pattern applied either, **this is a low-priority, optional cleanup — not a functional necessity, and not something to present as a client-facing improvement.** The legacy client itself gets zero visible benefit either way.

### 1.6 Rate limiting (related but distinct from auth)

Rate limiting is not auto-scaling and not solved by adding more containers — it stops a *single client* from abusing the system regardless of capacity. The code already has a toggle for this (`RateLimitOptions`), just not enabled.

Recommended configuration:
- **Login/authenticate endpoints:** 10 requests/minute per client — stops brute-force credential guessing.
- **All other endpoints:** 30 requests/minute per client — stops one client starving the gateway.

(Azure also offers this as a managed feature via API Management, as an alternative delivery mechanism — same concept, different implementation.)

---

## Part 2 — Auto-Scaling

### 2.1 What auto-scaling means, simply

Auto-scaling = a system that watches incoming load and automatically adds more "workers" (containers) when busy, and removes them when quiet — without a human doing it manually.

**Horizontal vs. vertical scaling:**
- **Vertical** = making one machine bigger (more RAM/CPU). Has a hard ceiling.
- **Horizontal** = running more copies (containers) side by side. This is what "auto-scaling" means here.

### 2.2 Where we actually stand today (verified against the code)

- Docker is already fully set up: `src/Api/Dockerfile`, `frontend/Dockerfile`, `docker-compose.yml`.
- But right now it's **one fixed container**, run locally via `docker-compose` — there is no cloud deployment target and no scaling rule of any kind yet.

So the containerization groundwork is done; the scaling and cloud-hosting layer is the genuinely open research item.

### 2.3 Azure hosting options compared

| Option | What it is | Fit for us |
|---|---|---|
| **Azure Container Apps** | Container-first hosting; supports scale-to-zero (cost drops to near-zero when idle) and easy container wiring | **Best fit** — we already have Docker images ready to go |
| Azure App Service | Older, mature platform; containers are a bolt-on feature, not its core design | Workable, but not built around containers the way our system already is |
| AKS (Kubernetes) | Built for large numbers of interdependent microservices; needs a dedicated infra/DevOps team to run well | Overkill for us right now |

**"Directly deploy" (i.e., publishing code straight to Azure without Docker)** is a real alternative in general, but not relevant here — we already containerize, so only Docker-friendly options matter.

**On "is our API an enterprise system"?** Yes and no, depending on what's meant:
- By *importance/sensitivity* (patient data, multiple backend systems routed through one gateway) — yes, this is a serious, enterprise-grade concern.
- By *scale/complexity* (number of interdependent services) — not yet. One gateway routing to four systems is not Kubernetes-scale.

**Recommendation: Azure Container Apps now. Revisit Kubernetes only if scale/complexity genuinely grows.**

### 2.4 Cost and reliability talking points

- Container Apps' consumption-based pricing means cost scales down automatically when idle — directly supports the "no manual intervention" goal.
- Built-in health checks mean unhealthy containers restart automatically without anyone paging in.

### 2.5 Region — explicitly an open question

Which Azure region to deploy in is **not yet decided** and is separate from the scaling question — region only affects where the compute physically runs (latency), while the auto-scaling mechanism works identically in any region. This should be confirmed with Dr. Javad directly rather than assumed.

### 2.6 HTTPS / transport security — an honest caveat

Canonical/new endpoints use HTTPS. Some legacy connection paths (e.g. HISO's internal routing) should be **verified, not assumed**, to be encrypted — this document does not claim blanket HTTPS coverage across every legacy path.

---

## Summary for the presentation

1. We already have JWT-based auth on canonical endpoints, and Entra ID code already built — this is a maturity/completion story, not a from-scratch build.
2. A real gap exists in how the Entra ID code validates callers today; the fix is scoped and well understood (see suggestions doc).
3. Legacy endpoints deliberately keep their exact current behavior — no client changes, ever.
4. Docker is done; the missing piece is cloud deployment + auto-scaling rules, and Azure Container Apps is the recommended target.
5. Region and Entra tenant setup are the two genuinely open decisions that need Dr. Javad / org-level input.
