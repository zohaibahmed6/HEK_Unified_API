# Enterprise Architecture Document — Unified Healthcare API

## 1. Document Control

| Field | Value |
|---|---|
| Title | Enterprise Architecture Document — Unified Healthcare API (consolidating HISO, KARO/HSS, ERMS/COL) |
| Version | 1.0 (Draft, pending client confirmation of remaining open items) |
| Date | 2026-07-19 |
| Author | Enterprise Architecture Review Board, via conversation with Zohaib Ahmed |
| Source SRS | `SRS_UnifiedHealthcareAPI.md` v1.0 |
| Companion document | `Unified-Healthcare-API_ADRs.md` (ADR-001 through ADR-011, full rationale) |
| Status | Draft — 4 open items remain (Section 26); ready for Database Architecture / API Contract Design to begin against everything else |

---

## 2. Executive Summary

This document defines the target architecture for consolidating three legacy NZ clinical-integration systems — HISO (WCF SOAP, ACC45 accident-claim forms), KARO/HSSWebAPI (HSS portal bridge), and ERMS/ERMSWebAPI (eReferrals plus the COL/Pegasus claiming integration) — into a single Unified Healthcare API. All three legacy systems share the same underlying Indici PMS data (sharded across multiple existing physical database servers, not one shared database as originally assumed), the same authentication anti-pattern (no real credential exchange), and near-total duplication of clinical read endpoints.

The guiding posture for this consolidation, set explicitly by the stakeholder, is **preserve-then-extend**: merge the three codebases into one canonical implementation per capability (closing the duplication problem), while changing as little as possible about existing database structure, server addressing, and consumer-facing contracts. New capabilities — real authentication, engine-level tenant isolation, HTTPS enforcement — are built in but shipped as configuration switches, validated against legacy-equivalent behavior first, then enabled incrementally. This trades some short-term security exposure (explicitly accepted and documented, Section 16) for a much lower-risk migration path and zero required changes for existing consumers (HSS Portal, ERMS eReferrals, COL/Pegasus, HealthLink-style form engines).

## 3. Architecture Goals

- Eliminate duplicated implementations of the same clinical/financial capability across three codebases (SRS §5.2).
- Close confirmed security gaps (no real auth, hardcoded secrets, unencrypted HISO transport) without requiring any change from existing consumers, and without a risky one-time data migration.
- Support 24/7 operation with no acceptable downtime (stakeholder-confirmed, revising the SRS's originally-unconfirmed availability target).
- Enable practice onboarding via configuration alone, with no code redeployment (FR-TEN-01, FR-ADMIN-01).
- Provide a validated, staged path from legacy-equivalent behavior to fully modernized behavior, rather than a single high-risk cutover.

## 4. Business Drivers

Three independently-maintained, duplicated integrations against the same practice-management data; a confirmed history of critical security gaps in all three (hardcoded secrets, no real authentication, PHI logged in plaintext, one active public-IP credential exposure); a stated need to onboard new practices without redeployment; and end-of-investment legacy technology (.NET Framework 4.8, WCF, OWIN) with no first-class modern hosting path (SRS §2, §4.4).

## 5. Current System Overview

See SRS §4 for full detail. Summary: HISO (WCF SOAP, session-GUID identified, ACC45 forms), KARO/HSSWebAPI (ASP.NET Web API 2, 24-endpoint "fat controller," HSS portal consumer), ERMS/ERMSWebAPI (ASP.NET Web API 2, XML eReferrals `APIController` plus JSON `COLController` for Pegasus claiming). Confirmed production topology (stakeholder-corrected from the SRS's original "one shared database" assumption, ADR-001): ERMS and KARO/HSS run from a single app server each, routing to multiple database servers depending on practice; HISO runs on multiple IIS servers, each historically bound to one specific database server. Some database servers already host more than one practice's data together.

## 6. Target Architecture

One unified API, deployed as a Docker-containerized service (ADR-005), fronted by a load balancer. Practice data continues to live on its existing database servers (no consolidation — ADR-001); a small, dedicated tenant/practice registry database tells the unified API which server to route each request to, replacing the legacy hardcoded per-practice connection-string lists. Login for all consumers is validated by a managed identity service (Entra ID, ADR-002), fronted by a compatibility layer that preserves every existing consumer's current request shape exactly. HISO's existing session-GUID mechanism and per-server addressing are preserved as-is (ADR-004, ADR-007), with an expiry and audit logging added. ERMS and KARO/HSS retain a single-designated-server model, backed by an active/standby failover pair rather than the full load-balanced fleet (ADR-011). Every new behavior described above ships as an off-by-default configuration switch (ADR-008), so the merged system can first be validated against legacy-equivalent behavior before any new capability is enabled.

## 7. Architecture Principles

- No consumer of the legacy systems (HSS Portal, ERMS eReferrals, COL/Pegasus, HealthLink-style form engines) is required to change its integration, with one narrow exception (HISO's endpoints move from `http://` to `https://` — ADR-009).
- No database schema or structure changes; build entirely on the existing schema (ADR-010).
- Authorization is decided structurally (by which credential/entry-point authenticated a request), never by a value the caller supplies itself (ADR-003).
- Every new capability is validated in a legacy-equivalent, switched-off state before being enabled (ADR-008).
- One canonical implementation per capability — consolidation must not silently expand any consumer's access beyond what it originally had (ADR-003's consumer-scoping rule).

## 8. Domain Decomposition

Per the SRS's functional-requirement groupings (§8): Patient/Demographics, Clinical Notes, Conditions/Diagnoses, Medications, Lab/Radiology Results, Documents/Attachments, Billing/Invoicing (including ACC45 and COL/Pegasus), Tenant/Administration, and a cross-cutting Idempotency concern (FR-IDEM-01). These map cleanly onto the unified API's canonical endpoints; none of the three legacy systems' domain boundaries were found to conflict with this grouping (SRS §6.1).

## 9. Module Boundaries

The Tenant/Administration domain (registry, routing, onboarding) sits below and is depended on by every other domain. Authentication/Authorization is a cross-cutting layer all domains sit behind, not a domain of its own. Document generation/storage (HISO's Aspose rendering, the shared DMS/AWS routing pattern) is a shared service consumed by ACC45, Documents, and any domain that produces attachments — not duplicated per domain.

## 10. API Strategy

REST/JSON direction confirmed by the SRS (§13.1), replacing HISO's SOAP transport. The specific response-contract shape (one canonical format vs. per-consumer adapters, reconciling KARO's JSON vs. ERMS's XML) is explicitly out of scope for this architecture phase — carried forward as a named handoff item to API Contract Design (SRS §13.3).

## 11. Integration Architecture

External dependencies per SRS §16: Indici PMS (system of record, integration preserved as-is), DMS/AWSDoc (document storage, consolidated into one abstraction), Aspose (HISO's document rendering, refactor recommended but tool choice not re-litigated here), and the still-unconfirmed Azure ERMS mirror and COL/Pegasus consumer liveness (Section 26). No message-queue or background-service infrastructure exists today; the SRS confirms this is required net-new capability at the target scale (§16) but no specific technology has been chosen — deferred to Infrastructure build-out once a cloud host is selected.

## 12. Data Architecture

SQL Server (confirmed platform, no migration away — SRS §14.1). Data remains sharded across existing physical database servers; the unified API adds a routing layer (a dedicated tenant/practice registry database) rather than consolidating into one database (ADR-001). No schema changes (ADR-010). Engine-level row isolation (row-level security) is designed but deferred as an off-by-default capability for later activation, not implemented in the initial rollout (ADR-001, ADR-008) — see Section 16 for the associated accepted risk.

## 13. Tenant Isolation Strategy

Practice identity is resolved via the central registry database, not by parsing composite ID strings per request (replacing KARO/ERMS's ~25–30 call sites of ad-hoc delimiter-splitting, SRS §8.2). Onboarding a new practice is a single-row insert into the registry — no redeployment (FR-TEN-01, FR-ADMIN-01). On database servers hosting multiple practices, isolation currently relies on application-layer query filtering; row-level security is built and ready to enable later (ADR-001).

## 14. Authentication Architecture

Real credential validation via a managed identity platform (Microsoft Entra ID, ADR-002), fronting a compatibility adapter that preserves every legacy consumer's exact current request shape. Each legacy system's shared service account (`hsslive` for HSS, equivalent accounts for ERMS/COL) is registered as a real service-account credential. HISO retains its existing session-GUID mechanism, now with an enforced 12-hour expiry (ADR-004) — treated as a resource-scoped credential in its own right, not retrofitted with a new login step. The entire authentication layer, including whether it's active at all, is a configuration switch defaulted off pending staged testing (ADR-008) — see Section 16 for the accepted risk this carries.

## 15. Authorization Architecture

Resource-scoped tokens, carrying claims for one patient + one encounter + one practice (mirroring KARO-BR-05/ERMS-BR-06's existing design intent, rebuilt on real signed tokens). Origin scope (which legacy system a caller is standing in for) is determined structurally — by which service account or entry point authenticated the request — never by a self-reported field in the payload (ADR-003). COL/Pegasus receives its own origin-scope, distinct from ERMS's other eReferrals functions, given its financial `SaveInvoice` write.

## 16. Security Architecture

HTTPS enforced on every endpoint, including HISO's existing addresses (currently plain HTTP — a concrete gap found via live compliance-environment traffic, not just SRS analysis; ADR-009). Secrets management, CORS policy, and input validation follow the SRS's §12 requirements and are not re-litigated here.

**Explicitly accepted risk, recorded per the stakeholder's own decision (ADR-008):** authentication itself, and row-level tenant isolation (Section 13), are both configuration switches defaulted off, to support staged validation against legacy-equivalent behavior. This means the system *can* currently be deployed to production in the same effectively-unauthenticated state as the three legacy systems it replaces, if these switches are not manually enabled before go-live. This is not an oversight — it was explained to the stakeholder and knowingly accepted — but it is the single highest-priority item in Section 26's open questions, and should have a named owner and a firm enable-by date before any production launch.

## 17. Logging & Observability

Structured, correlated logging replacing the legacy `Logger.dll` pattern; PHI/secret redaction; failed authentication/session-lookup attempts logged as real security events rather than silently swallowed (closing HISO's specific confirmed gap, ADR-004). Health checks, metrics, and alerting are net-new (SRS §15) — not present in any legacy system, and not further detailed here pending the cloud-host decision (Section 26).

## 18. Performance & Scalability

No specific latency/throughput SLA is confirmed anywhere in the SRS; the only stated target is 10,000 concurrent users (SRS §5.5). The registry-based routing model (Section 13) removes the legacy per-request delimiter-parsing and connection-string-selection overhead. Caching and search-engine infrastructure are assessed as low-impact per the decision catalog (no SRS evidence of a latency SLA or free-text search requirement) and are not included in the initial design.

## 19. High Availability

Full platform target: 24/7, zero acceptable downtime, revising the SRS's originally-unconfirmed availability requirement (ADR-006) — two live locations running at once (active-active), not just a passive backup. **Exception:** ERMS and KARO/HSS are restricted to a single designated server at a time, matching their legacy single-server deployment, but backed by an active/standby pair with load-balancer health-check-driven automatic failover, rather than the full N-way load-balanced model the rest of the platform uses (ADR-011).

## 20. Disaster Recovery

No RTO/RPO figures were available from the original SRS/analysis; the stakeholder has since confirmed a 24/7, zero-downtime operating target (Section 19), which sets the practical DR bar even without separately-stated RTO/RPO numbers. Formal RTO/RPO figures remain an open item for the business to confirm (Section 26) before DR tooling/runbooks are finalized.

## 21. Infrastructure Architecture

Docker-containerized deployment (ADR-005); specific cloud provider/host is an open decision (Section 26), kept deliberately unresolved so the architecture stays portable. The tenant/practice registry database is a new, small, critical shared dependency and should receive infrastructure-level availability protection independent of any single practice database's health (ADR-001).

## 22. Deployment Architecture

Several identical containers behind a load balancer for the general platform (ADR-005/ADR-006); a dedicated active/standby pair specifically for ERMS/KARO-HSS (ADR-011); HISO's existing per-server addressing preserved, with routing decided by which address was called rather than by data in the request (ADR-007). Every new behavior in this architecture is a configuration toggle, defaulted off, enabling a staged rollout starting from legacy-equivalent behavior (ADR-008) — see `references/decision-catalog.md`'s "Rollout Strategy for New Capabilities" for the underlying pattern.

## 23. Technology Stack

| Layer | Choice | Source |
|---|---|---|
| Database | SQL Server (existing, unchanged) | SRS §14.1, ADR-010 |
| Identity/Auth | Microsoft Entra ID | ADR-002 |
| Containerization | Docker | ADR-005 |
| Cloud/host | Open — Section 26 | — |
| API style | REST/JSON (contract shape deferred to API Contract Design) | SRS §13.1, §13.3 |
| Messaging/queue | Deferred — required net-new per SRS §16, technology not yet chosen | SRS §16 |

## 24. Architecture Decision Records

See the companion document `Unified-Healthcare-API_ADRs.md` for the full text of ADR-001 through ADR-011, including context, options considered, and evidence for each.

## 25. Risks

| Risk | Mitigation |
|---|---|
| Security and tenant-isolation switches shipped off by default could reach production unflipped (Section 16) | Named owner and enable-by date required before go-live — currently the top open item (Section 26) |
| ERMS/KARO-HSS's single-designated-server model has less inherent redundancy than the rest of the platform | Mitigated by the active/standby failover pair (ADR-011), but still worth revisiting if load on that pair grows |
| Row-level security deferred means shared-tenant database servers currently rely on application-code discipline alone | Row-level security is built and ready — enabling it should be prioritized early in the staged rollout, not left indefinitely |
| Azure ERMS mirror and COL/Pegasus consumer liveness unconfirmed | Must be verified with the client before any related traffic is assumed safe to drop or preserve as-is (Section 26) |
| Every table needing row-level protection is assumed to already carry a practice-identifying column | Not yet verified across every table — confirm during Database Architecture phase (ADR-010) |

## 26. Open Questions

1. **Give the security/isolation toggles (ADR-008) a firm production enable-by date and a named owner** — currently the highest-priority open item.
2. **Cloud provider / container host** — deliberately left open; Docker is locked in, where it runs is not.
3. **Azure ERMS mirror and COL/Pegasus consumer liveness** — needs a direct answer from the client.
4. **ERMS's hardcoded PHO-override parity with KARO** (does ERMS have the same practice-302 override) — needs verification against ERMS's live behavior.

## 27. Acceptance Criteria

This architecture is ready to hand off to Database Architecture / API Contract Design when: all four items in Section 26 are resolved or explicitly deferred with an owner and date; the assumption in Section 25 (every protected table has a practice-identifying column) is verified; and the security/isolation toggles have a committed enable timeline. Everything else recorded in ADR-001 through ADR-011 is locked and does not require further architecture-phase decisions.

## 28. Appendix

See `Unified-Healthcare-API_ADRs.md` for full decision rationale, and `SRS_UnifiedHealthcareAPI.md` for the underlying requirements, business rules, and evidence citations this document builds on.

---

# Architecture Review Board Validation

| Category | Result | Rationale | Recommendation |
|---|---|---|---|
| Security | **WARNING** | Real gaps (no HTTPS on HISO, no real auth) are correctly identified and designed for, but both authentication and row-level isolation ship off by default with no automated production gate (ADR-008) | Assign an owner and date to flip these on before go-live (Section 26, item 1) |
| Scalability | **PASS** | Registry-based routing removes the legacy per-request parsing/connection-selection bottleneck; 10,000-user target has no contradicting design element | — |
| Availability | **PASS** | 24/7 active-active target explicitly addressed platform-wide, with a deliberate, justified exception for ERMS/KARO-HSS backed by its own failover pair | — |
| Performance | **WARNING** | No latency/throughput SLA exists to design against; caching/search deferred appropriately, but this should be revisited once real numbers exist | Request specific performance targets from the business |
| Maintainability | **PASS** | Domain decomposition follows the SRS's own functional grouping; one canonical implementation per capability | — |
| Reliability | **WARNING** | HISO's fallback-search routing (ADR-007) and the two-step DMS+PMS write pattern (SRS §17.4 risk) both need explicit failure-handling design, not yet detailed at implementation level | Define these explicitly in the Database Architecture phase |
| Modularity | **PASS** | Module boundaries map cleanly to the SRS's functional-requirement groupings; no invented decomposition | — |
| Observability | **WARNING** | Structured logging and audit-event logging are designed, but concrete metrics/health-check tooling is deferred pending the cloud-host decision | Revisit once Section 26, item 2 is resolved |
| Disaster Recovery | **WARNING** | No formal RTO/RPO figures exist; the 24/7 target implies a strict bar, but without documented numbers, DR tooling/runbooks can't be finalized | Obtain formal RTO/RPO from the business |
| Cost Awareness | **PASS** | Active-active HA and the ERMS/HSS failover pair are both deliberate, justified tradeoffs tied to an explicit 24/7 requirement, not defaulted to for their own sake | — |
| Operational Complexity | **PASS** | The design avoids introducing infrastructure (message queues, search engines, microservices) not called for by any confirmed requirement | — |
| Future Extensibility | **PASS** | The registry-based routing and origin-scoping patterns generalize cleanly to new practices and new consumer types without structural rework | — |

## Architecture Readiness Score

**8 PASS / 4 WARNING / 0 FAIL.** Not blocked, but not fully closed out either — none of the WARNINGs are structural flaws, they're all either explicitly-accepted, tracked risks (the security/isolation toggle) or figures that must come from the business (performance targets, RTO/RPO) rather than from architecture work itself. Recommend resolving Section 26's four open items, in priority order, before treating this as final.

## Recommended Next Phase

**Database Architecture Design**, informed directly by this document and ADR-001/ADR-010 (existing schema, registry database design, row-level security readiness) — followed by or run in parallel with **API Contract Design**, which should resolve Section 10's deferred response-contract-shape decision (SRS §13.3).
