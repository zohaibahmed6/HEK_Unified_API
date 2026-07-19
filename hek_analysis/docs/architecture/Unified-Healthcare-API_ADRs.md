# Unified Healthcare API — Architecture Decision Records

**Source SRS:** `SRS_UnifiedHealthcareAPI.md` v1.0 (Draft), 2026-07-18
**Deciders:** Zohaib Ahmed (stakeholder), Enterprise Architecture Review Board
**Date:** 2026-07-19
**Status:** Decisions below are stakeholder-approved based on direct conversation. Items marked "Open" in Section 7 still need confirmation before the Database Architecture / API Contract Design phases begin.

---

## ADR-001: Pooled Multi-Tenancy Across Existing Sharded Database Servers, with Row-Level Security

**Status:** Approved
**Deciders:** Zohaib Ahmed

### Context
FR-TEN-01 and FR-ADMIN-01 require that a new practice be onboardable via a data change alone, with no code redeployment — explicitly replacing HISO's fixed 4-connection model and KARO/ERMS's per-practice `Web.config` connection-string convention (12 and 15 targets respectively).

Critically, the SRS's assumption of "one shared Indici PMS database" (§4.4) does not match the real production topology, confirmed directly by the stakeholder: ERMS and KARO/HSS run from one app server that already routes to **different physical database servers** depending on the calling practice; HISO runs on multiple IIS servers, each bound to its own specific database server. Some database servers already host **more than one practice's data together**. This is a material correction to the SRS's stated assumption and takes priority as direct stakeholder knowledge of the live system.

### Decision
Do not attempt to consolidate practice data into a single physical database (too large a migration risk, not required by any confirmed requirement). Instead:
1. Introduce one small, dedicated **tenant/practice registry database**, separate from every practice database, holding one row per practice with which physical database server it lives on (replacing the hardcoded `Web.config` connection-string lists).
2. The unified API looks up the registry on each request to route to the correct target database server.
3. **Row-level security (RLS)** on shared database servers is deferred — not implemented right now. Per ADR-008, it's treated the same as every other new capability: built to be turned on later, but off for the initial rollout. Until it's switched on, isolation on servers hosting more than one practice depends entirely on the application code's own filtering logic, the same as the legacy systems today — this is a known, accepted gap during the initial phase, not a permanent decision to skip it.

### Options Considered
- Silo (database per tenant) — rejected: conflicts with the no-redeployment requirement and would require provisioning a new database per practice.
- Full consolidation into one shared database — rejected: real production data is already sharded across multiple physical servers; forcing a merge is a large, risky migration not called for by any requirement.
- Pooled/shared schema with application-code-only filtering — rejected: given all three legacy systems' confirmed history of basic security failures (hardcoded secrets, no real auth, wildcard CORS), relying solely on query-layer discipline for PHI isolation was judged too risky.
- **Chosen: route to existing physical shards via a central registry, RLS within any shared shard.**

### Consequences
Onboarding a new practice becomes a single-row insert in the registry database — no redeploy, no other database touched. No risky one-time data migration is required. The registry database becomes a new critical, shared dependency and should get its own strong availability protection, since a routing failure there would affect all practices at once. RLS policies must be kept in sync with schema changes going forward — this should be a standing checklist item in the Database Architecture phase, not a one-time setup.

### Evidence / Approval
SRS §5.6, §8.15 (FR-TEN-01, FR-ADMIN-01), §12 (PHI handling), §4.4. Production-topology correction and RLS necessity confirmed directly by stakeholder, 2026-07-19.

---

## ADR-002: Real Authentication via a Managed Identity Service, with a Compatibility Layer for Existing Consumers

**Status:** Approved
**Deciders:** Zohaib Ahmed

### Context
§12.1 mandates real, framework-level authentication, replacing HISO's credential-less GUID session and KARO/ERMS's hand-rolled bearer tokens (KARO's framework auth was never switched on; ERMS re-validated a hand-rolled token against the database on every call and logged it in plaintext). §16 confirms no real identity-provider integration exists today in any of the three systems.

The stakeholder confirmed a hard constraint: existing consumers (HSS Portal, ERMS eReferrals platform) must not be required to change anything on their end. Their existing `Authenticate` call (shared service-account credentials, e.g. `hsslive`, plus `PatientId`/`EncounterId`/`UserId`/`System`/`Pho` in the payload) is a system-to-system call from each portal's own backend, not an individual clinician's login screen.

### Decision
Adopt a managed identity platform (e.g. Entra ID / Okta / Auth0 — specific vendor is an Open item, Section 7) for real credential validation, fronted by a **compatibility adapter** that preserves each legacy consumer's existing request shape exactly:
- HSS Portal and ERMS eReferrals continue sending their current `Authenticate` payload unchanged.
- Internally, each legacy service account (`hsslive`, and the equivalent for ERMS/COL-Pegasus) is registered as a real service-account credential in the identity service, replacing the current hardcoded/ad-hoc password comparison.
- On successful validation, the platform issues a real, signed, short-lived internal token — this is the credential that flows through the rest of the system.

### Options Considered
- Managed identity platform (chosen) — offloads MFA/credential-security/breach-detection to a vendor with dedicated security engineering, appropriate given all three legacy systems' confirmed history of basic auth failures.
- Self-hosted OIDC/OAuth2 — rejected: the team would again own security-critical code, the exact category where the legacy systems failed repeatedly.
- API-key/client-credentials only — rejected: real human end users (providers, receptionists) authenticate interactively via consuming portals; a pure machine-only model doesn't fit.

### Consequences
Zero integration changes required for HSS Portal or ERMS eReferrals. The team gains a professionally-maintained login system instead of building a fourth in-house attempt. A translation/adapter layer must be built and maintained at the API edge — a small, deliberate piece of legacy-compatibility code, not a shortcut around real security.

### Evidence / Approval
SRS §12.1, §16, KARO-BR-07/08, ERMS-BR-06/07. Backward-compatibility constraint and real `Authenticate` payload shape confirmed directly by stakeholder, 2026-07-19.

---

## ADR-003: Resource-Scoped Tokens with Structurally-Determined Origin Scope

**Status:** Approved
**Deciders:** Zohaib Ahmed

### Context
FR-AUTH-02 requires authorization scoped to the specific patient+encounter+practice combination, not just "is this caller logged in." KARO-BR-05 and ERMS-BR-06 show the legacy systems already re-validate every call against exactly that tuple — the correct *design intent*, implemented insecurely (a database round-trip per call, no real cryptographic guarantee).

The stakeholder additionally required that the unified API's single, merged implementation must still prevent a caller originally scoped to one legacy system (e.g. KARO/HSS) from reaching another legacy system's functions (e.g. ERMS-only or HISO-only capabilities) — without reintroducing duplicated code per system.

An earlier draft of this decision proposed determining origin scope from a self-reported `"System"` field in the caller's request payload. This was corrected during review: **a security-relevant scope decision must never be based on a value the caller supplies themselves** — it must be anchored to something the platform verifies at authentication time.

### Decision
1. **Resource-scoped tokens**: issued tokens carry claims scoped to one patient + one encounter + one practice, mirroring KARO-BR-05/ERMS-BR-06's existing (insecure) design intent, rebuilt on real signed tokens.
2. **Origin scope determined structurally, by which credential/entry-point authenticated the request, not by a self-declared field:**
   - HSS Portal's dedicated service account is registered as "KARO/HSS-origin" in the identity service; any token minted from that login automatically carries that origin claim.
   - ERMS eReferrals and COL/Pegasus each get their own dedicated service account(s), tagged "ERMS-origin" accordingly (whether COL/Pegasus needs a distinct sub-scope from ERMS's other eReferrals functions is an Open item, Section 7).
   - HISO has no login step at all — but it also has only one possible entry point (the `tblHealthLinkSession`-based session-GUID lookup). Anything resolved through that specific path is unambiguously "HISO-origin" by construction; no new field or consumer-side change is required.
3. One canonical implementation per capability is preserved (satisfying the SRS's consolidation goal, e.g. FR-PAT-01) — origin scope restricts *access* to functions, it does not duplicate the functions themselves.

### Options Considered
- RBAC — rejected: cannot natively express "this specific patient+encounter+practice," the exact granularity FR-AUTH-02 requires.
- ABAC/policy engine — considered but not chosen now: more upfront design/testing investment than the resource-scoped-token approach, which directly reuses a design pattern already proven in KARO/ERMS's business rules; may be revisited if per-practice exceptions (e.g. KARO-BR-04-style overrides) turn out to be numerous.
- Trusting a self-reported `"System"` field for origin scope — rejected on security-review: caller-supplied values must not drive authorization decisions.

### Consequences
A single merged implementation is preserved — no duplicated per-legacy-system code. A consumer migrating onto the unified API does not gain access to capabilities it never had before, avoiding an unintended privilege expansion. HISO requires no new credential step to participate in origin-scoping, since its single entry point already implies its origin.

### Evidence / Approval
SRS FR-AUTH-02, KARO-BR-05, ERMS-BR-06, FR-PAT-01 (consolidation intent). Backward-compatible origin-scoping requirement, the `"System"` field correction, and the HISO single-entry-point resolution all confirmed directly by stakeholder, 2026-07-19.

---

## ADR-004: HISO Session Handling — Treat Existing Session GUID as a Scoped Credential, Add Expiry and Failure Auditing

**Status:** Approved
**Deciders:** Zohaib Ahmed

### Context
HISO's session is not a login in the traditional sense: Indici itself creates a row in `[Appointment].[tblHealthLinkSession]` (keyed by ProviderID, PatientID, AppointmentID, PracticeID) when a provider initiates a HealthLink-style action, and hands the resulting `SessionGUID` to the external HealthLink-style form engine. HISO's SOAP operations resolve that GUID back into full context per call (HISO-BR-01). Confirmed gaps: no expiry is enforced (HISO-BR-01/02 describe resolution and failure handling but no expiry), and failed lookups are swallowed by generic exception handling rather than logged as security events (`ComparisonReport.md` §9; SRS security requirements §12.8).

The stakeholder confirmed that the HealthLink-style form engine and Indici's session-creation behavior must not change.

### Decision
Treat the existing `SessionGUID` as what it already functionally is — a resource-scoped credential tied to one provider+patient+appointment+practice, equivalent in spirit to the resource-scoped tokens in ADR-003 — rather than inventing a new login flow for HISO. On top of the existing, unchanged mechanism:
1. Add a real, enforced expiry to each session: **12 hours**, matching ERMS's existing expiry window, for consistency across the unified platform.
2. Replace the current swallowed-exception pattern on failed/invalid session lookups with explicit security-event logging (feeding the platform's audit-logging requirement, §12.8), so failed access attempts become visible rather than indistinguishable from generic errors.
3. No change is required to Indici's session-creation behavior or to the HealthLink-style form engine's calling pattern.

### Options Considered
- Introduce a new credential-exchange step for HISO (a service account, similar to HSS Portal's) — rejected for now: the existing session-GUID mechanism already provides equivalent scoping (provider+patient+appointment+practice), and the stakeholder confirmed no change to the HealthLink engine or Indici's session creation is acceptable. Would be revisited only if the GUID generation itself is found to be insufficiently random — flagged as an Open item.
- Leave sessions with no expiry (status quo) — rejected: a non-expiring access credential is a standing security risk explicitly worth closing.

### Consequences
No integration changes required for the HealthLink-style engine or Indici's PMS-side session creation. HISO gains parity with ERMS's expiry behavior and closes a real audit-logging gap. The randomness/unguessability of the existing `SessionGUID` generation should be verified as a follow-up (Open item, Section 7) — this ADR assumes it is a cryptographically strong GUID, consistent with its naming, but this has not been independently confirmed.

### Evidence / Approval
SRS HISO-BR-01/02, §12.8, `ComparisonReport.md` §9. Session-creation mechanism (the `tblHealthLinkSession` query) and the "no consumer-side changes" constraint provided directly by stakeholder, 2026-07-19.

---

## ADR-005: Docker-Based Deployment, Load-Balanced Across Identical Instances

**Status:** Approved
**Deciders:** Zohaib Ahmed

### Context
The SRS calls for moving off old-style `.csproj`/`packages.config`/`HintPath`-based builds onto a self-contained, reproducible build (§11, Deployment row), and off the legacy WCF/OWIN hosting stack generally (§13.1, §17.2 Phase B item 6). Legacy HISO ran on multiple IIS servers, each hardwired to a specific database server; legacy KARO/ERMS ran from a single app server routing to multiple databases via connection strings. With tenant/database routing now centralized in the registry (ADR-001), neither legacy hosting pattern's original constraints still apply.

### Decision
Package the unified API as a Docker container, deployed as multiple identical running instances behind a load balancer. Any instance can serve any practice's traffic, since routing is resolved via the registry (ADR-001) rather than by which physical server the instance happens to be. Specific cloud provider/host is an Open item (Section 7) — the architecture is kept cloud-agnostic pending that decision.

### Options Considered
- One server per database (HISO's legacy pattern) — rejected: no longer necessary once routing is centralized; doesn't scale cleanly as databases are added.
- Single server, no redundancy — rejected: no failover if that one instance goes down.
- **Chosen: several identical containers, load-balanced.**

### Consequences
Satisfies the SRS's modernization requirement to move off the legacy hosting stack. Provides a natural fit with the availability decision in ADR-006 (multiple identical, replaceable instances). The specific container orchestration platform and hosting provider remain to be decided.

### Evidence / Approval
SRS §11 (Deployment), §13.1, §17.2. Docker as the deployment technology and the decoupling from HISO's legacy per-server-per-database pattern confirmed directly by stakeholder, 2026-07-19.

---

## ADR-006: Maximum High Availability — Active-Active, Zero-Downtime Target

**Status:** Approved (supersedes an earlier "balanced" draft of this ADR)
**Deciders:** Zohaib Ahmed

### Context
§11 states plainly that no uptime/SLA target was documented anywhere in the source analysis for any of the three legacy systems. An earlier draft of this ADR therefore proposed a "balanced" profile (backup region, quick-start recovery) as a provisional, qualitative judgment pending real numbers. The stakeholder has since provided the missing number directly: **24/7 operation, no acceptable downtime.**

### Decision
Adopt the maximum-protection profile: two live locations running at once (active-active), with a hot standby posture — not just a backup on standby, but a second location already handling real traffic so failover is effectively instant.

### Options Considered
- Balanced (multi-region active-passive, pilot-light DR) — superseded: this was the right call *absent* a stated requirement, but the stakeholder has now stated a zero-downtime requirement directly, which this profile doesn't fully satisfy.
- **Chosen: active-active, hot standby**, now justified by an explicit stakeholder-provided target rather than a qualitative guess.

### Consequences
This is a real cost and complexity increase over the earlier "balanced" recommendation (cross-region data consistency, conflict resolution, running duplicate live infrastructure at all times) — but it is no longer a guess dressed up as a decision; it directly reflects a stated business requirement.

### Evidence / Approval
Confirmed directly by stakeholder, 2026-07-19: "we need it 24/7 no downtime acceptable." This closes the RTO/RPO open item from the previous round.

---

## ADR-007: HISO Database Routing via Existing Per-Server Addresses

**Status:** Approved
**Deciders:** Zohaib Ahmed

### Context
Unlike KARO/ERMS, whose composite `encounterId`/`EncounterId` strings embed the practice directly (enabling registry-based routing per ADR-001), HISO's callers send only a bare `SessionGUID` with no practice or environment indicator in the message itself. Today, HISO's routing "just works" because each HealthLink-style engine already calls a specific, distinct HISO server address, and that server is hardwired to one specific database. The stakeholder confirmed the HealthLink engines' calling behavior must not change.

### Decision
Preserve HISO's existing per-server addresses as the routing signal, rather than requiring a practice identifier inside the message. The unified API exposes the same distinct addresses HISO already uses today; internally, "which address was called" determines which database to query for that session, the same way ERMS's `System` field and dedicated service accounts determine origin-scope (ADR-003) — routing decided structurally by which door was used, not by information the caller would need to newly provide. A fallback search across other known databases is triggered only if a session code doesn't resolve on its expected database (logged as an anomaly, not the normal path).

### Options Considered
- Require a practice identifier in the HISO call — rejected: would require changing the HealthLink engines' behavior, which the stakeholder ruled out.
- Broadcast/search all databases for every call — rejected as the primary mechanism: unnecessary overhead when the existing address already tells us the answer; kept only as a rare fallback.
- **Chosen: route by which existing address was called, fallback search only on mismatch.**

### Consequences
Zero change required for HealthLink engines or how HISO is currently addressed. The unified API must preserve every one of HISO's existing server addresses going forward (or provide equivalent routing), rather than consolidating to a single HISO endpoint.

### Evidence / Approval
Confirmed directly by stakeholder, 2026-07-19: HISO runs on multiple servers today (one per database), unlike ERMS/HSS's single-server, registry-routed model; "do not change anything," fallback approach explicitly approved.

---

## ADR-008: Configuration-Driven Rollout — All New Behavior, Including Security, Is Toggleable

**Status:** Approved (risk explicitly accepted by stakeholder — see Consequences)
**Deciders:** Zohaib Ahmed

### Context
The stakeholder's primary goal is a like-for-like merge of the three existing APIs first, with new capabilities (new login/identity service, origin-scoping, etc.) introduced afterward, one at a time, each independently tested — rather than a big-bang cutover to new behavior. The stakeholder explicitly requested that all new behavior be config-driven (settings-file-based, consistent with the legacy systems' `Web.config` convention), defaulted **off**, so the merged API can first be validated against old behavior before anything new is switched on.

The review board raised a direct concern: making real authentication (ADR-002/ADR-003) itself a toggle risks reintroducing, in production, the exact "no real security" gap this project exists to close — and recommended a hard block preventing production from starting with security toggled off. **The stakeholder considered this and explicitly chose to keep security itself configurable too**, intending to enable it once security testing passes, and declined the hard production block in favor of a manually-managed toggle.

### Decision
Every new capability introduced by this architecture — including authentication/security — is implemented as a configuration setting, defaulted off, so the system can run in "legacy-equivalent" mode first and have new features enabled incrementally. No automatic hard block prevents production from running with security toggled off; enabling it in production is a manual step the stakeholder will perform once satisfied by testing.

### Options Considered
- Security always-on, not configurable (board's original recommendation) — not chosen: stakeholder explicitly preferred a manual toggle to support incremental testing.
- Config toggle everywhere, with a hard block preventing production from starting with security off — offered as a middle ground; not chosen.
- **Chosen: config toggle everywhere, including security, no automated production block.**

### Consequences
This gives real flexibility for a staged, low-risk rollout and validation process, which is a genuine benefit. It also means **the system can currently be deployed to production with no real authentication, exactly like the three legacy systems it's replacing**, if the toggle is not manually switched on before go-live — this is a known, explicitly-accepted risk, not an oversight. Recorded here so it is visible to anyone reviewing this document, and so enabling security in production before real go-live is tracked as a required manual step, not assumed automatic.

### Evidence / Approval
Confirmed directly by stakeholder, 2026-07-19: "make it toggle too because i want first to execute it previous behaviour as it is then i turn on new features one by one and test them"; production hard-block explicitly declined: "make it configurable i will change it later if security test pass."

---

## ADR-009: Force HTTPS on Every Endpoint, Including HISO

**Status:** Approved
**Deciders:** Zohaib Ahmed

### Context
Live Postman compliance collections shared by the stakeholder show HISO's `FormSessionService.svc` is currently called over plain **HTTP**, not HTTPS (`http://compliancehiso.vitonta.com/...`). This means the session GUID and all patient/clinical data in HISO's SOAP calls travel unencrypted today — a concrete, evidence-confirmed gap beyond what the SRS's analysis-based review had visibility into. HSS, ERMS, and COL's compliance endpoints were confirmed running over HTTPS already.

### Decision
All endpoints in the unified API — including HISO's existing addresses (ADR-007) — must run over HTTPS only. No plain-HTTP path is preserved, even for backward compatibility; this is a transport-layer fix invisible to callers beyond the URL scheme.

### Consequences
The HealthLink-style form engines calling HISO's addresses will need their configuration updated to use `https://` instead of `http://` — this is the one small exception to "no consumer-side change," but is a minimal, one-time config update, not a behavior or contract change.

### Evidence / Approval
Confirmed directly from stakeholder-provided Postman compliance collections, 2026-07-19; addition explicitly approved by stakeholder.

---

## ADR-010: No Database Schema Changes — Build on Existing Structure Only

**Status:** Approved
**Deciders:** Zohaib Ahmed

### Context
Stakeholder confirmed the unified API must follow the same database structure the legacy systems already use — no new tables, no new columns, no schema redesign. This is a hard constraint on every other decision that touches data (ADR-001's row-level security in particular).

### Decision
All data-layer changes (routing, row-level security, etc.) must be implemented against the existing schema as-is. ADR-001's row-level security relies on a practice-identifying column already being present on any table it protects — confirmed present on at least `tblHealthLinkSession` (`PracticeID`), not yet confirmed across every table that would need it.

### Consequences
Low risk, low migration effort — consistent with the "don't change what already works" principle running through this whole design (ADR-007, ADR-008). **Open risk:** if any table needing protection turns out to lack a practice-identifying column, row-level security cannot be applied there without a structure change, which would conflict with this ADR — needs verification during the Database Architecture phase, not assumed clean.

### Evidence / Approval
Confirmed directly by stakeholder, 2026-07-19. Existing `PracticeID` column confirmed via the `tblHealthLinkSession` query shared earlier in this conversation.

---

## ADR-011: ERMS and HSS Functionality Restricted to One Designated Server

**Status:** Approved
**Deciders:** Zohaib Ahmed

### Context
Unlike HISO, which legacy ran on many servers (one per database, handled by ADR-007), ERMS and HSS/KARO each legacy ran from exactly one single server. The stakeholder wants that same single-server behavior preserved for now, even though ADR-005 otherwise deploys the unified API as several identical, load-balanced containers.

### Decision
ERMS and HSS functionality is enabled on exactly one designated server/instance at a time — but that designation is backed by an **active/standby pair**, not a single lone server, to resolve the tension with ADR-006's 24/7 zero-downtime requirement:
1. Two instances are configured for ERMS/HSS: one labeled `Role: Primary`, one `Role: Standby` (a configuration setting per ADR-008, not hardcoded).
2. Only the `Primary` instance actively serves ERMS/HSS calls; any other instance (including the standby, while inactive) blocks/rejects them — preserving the "one server handles this" behavior the stakeholder wants.
3. The load balancer (already part of ADR-005's design) continuously health-checks the primary. If it stops responding, the load balancer automatically flips the standby's role to `Primary` and redirects traffic to it — no manual config edit required at failover time. The static config labels declare intended roles; the load balancer's health checks are what make the switch actually automatic.

### Consequences
This gives ERMS/HSS a real automatic failover path while still matching the "single server handles this" shape the stakeholder wanted, rather than spreading them across the full fleet like HISO. The load balancer takes on one more responsibility (role-aware health-checked failover for this specific pair), which should be called out explicitly during the Infrastructure/Deployment build-out, not assumed automatic by default tooling.

### Evidence / Approval
Confirmed directly by stakeholder, 2026-07-19, matching legacy ERMS/HSS's confirmed single-server deployment.

---

## 7. Decisions Log — Follow-Up Round (2026-07-19)

| Item | Resolution |
|---|---|
| Identity-service vendor | **Microsoft Entra ID** |
| Container host / cloud provider | **Still open** — Docker is locked in; where it runs is not |
| Human logins vs. legacy service accounts (hsslive, ERMS/COL equivalents) | **One identity service, different account types** — not split into two separate systems |
| COL/Pegasus origin-scope | **Separate from ERMS's other eReferrals functions** — given it includes a financial `SaveInvoice` write |
| Token early-revocation ("kill switch" before natural expiry) | **Not included** — tokens simply expire naturally (see ADR-002/ADR-003) |
| `SessionGUID` randomness | **Confirmed strong** — not a weak generator |
| Compliance/data-residency regime | **NZ health privacy law applies** (NZ Health Information Privacy Code) |

## 8. Open Items — Still Not Confirmed

1. **Whether the Azure ERMS mirror and the COL/Pegasus consumer are still actually in production use** — carried forward from the SRS's own unresolved list (§22.4); needs a direct answer from the client. (The dormant DAL modules themselves are resolved — see below: retained for future use, not removed.)
2. **Specific cloud provider / container host** — deliberately left open per stakeholder decision; can be decided later without affecting anything already locked.
3. **ERMS's hardcoded PHO-override parity with KARO** (does ERMS have the same practice-302 override KARO has) — carried forward from the SRS's own unresolved list; needs verification against ERMS's live behavior.
4. **A concrete plan and timeline for switching the security toggle on in production** (see ADR-008) — currently a manual, undated step; recommend this get a firm owner and date before go-live, not left indefinite.

## 9. Additional Decisions — Follow-Up Round (2026-07-19, continued)

| Item | Resolution |
|---|---|
| Dormant DAL modules (BPAC, BPI, GP2GP, MHNAppointment, MHNHL7, Procare, Procon, Screening, UI, DMSAWS) | **Retained** — confirmed needed for future use; not removed, superseding the SRS's tentative "Remove, pending confirmation" |
| Uptime / downtime tolerance | **24/7, zero downtime** — see revised ADR-006 |
| HISO database routing (no practice identifier in its calls) | **Resolved — see ADR-007** (route by existing per-server address, fallback search on mismatch) |
| Rollout approach for all new capabilities, including security | **Config-driven, defaulted off, including security — see ADR-008** (risk explicitly accepted by stakeholder) |
