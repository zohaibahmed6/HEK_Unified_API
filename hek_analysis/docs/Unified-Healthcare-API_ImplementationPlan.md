# Implementation Plan — Unified Healthcare API

## 1. Document Control

| Field | Value |
|---|---|
| Project | HEK Unified Healthcare API (HISO + KARO/HSS + ERMS consolidation) |
| Phase | 11 — Implementation Planning |
| Status | Draft v1.0 |
| Date | 2026-07-19 |
| Inputs | SRS v1.0, Enterprise Architecture Document (EAD), ADR log (ADR-001–011), API Contract Design v1.1, `Unified-Healthcare-API_openapi.yaml` v1.1.0 |
| Phases folded in | Phase 8 (Security Architecture detail), Phase 9 (Logging & Observability Design), Phase 10 (Infrastructure Design) — by stakeholder decision, embedded as build tasks below rather than produced as standalone documents |

### Revision History

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-07-19 | Initial plan, agreed in chat 2026-07-19 |

---

## 2. Scope Framing — Read This First

This plan has two phases with very different character, and they must not be confused with each other:

**Day 1 — Build Sprint.** A single intensive session that produces a complete, working draft of the entire codebase: solution scaffolding, security core, all 42+ endpoints across 18 domain groups, and the compatibility adapters. This is achievable in a day because code *generation* is fast. It produces a real, runnable draft — not a production-ready system.

**Post-Sprint — Hardening.** Testing, security verification, load testing, and staged rollout. This is deliberately *not* compressed. The system handles PHI, carries a confirmed (and to-be-fixed) SQL injection history, and has a stated 24/7 / 10,000-concurrent-user target with zero existing automated tests anywhere in the legacy estate. Verifying those things takes real time regardless of how fast the code was written. Treating Day 1's output as "done" would be the single most expensive mistake available on this project — this plan is structured to make that distinction impossible to miss.

Nothing goes near real PHI until the hardening phase is complete.

---

## 3. Assumptions Locked In (2026-07-19 stakeholder Q&A)

| Decision | Value | Source |
|---|---|---|
| Target framework | **.NET 8 (LTS)** | Stakeholder decision — chosen over .NET 9 for the longer support window on a 24/7 production system |
| Architecture style | Clean/Onion: `Api`, `Application`, `Domain`, `Infrastructure`, `Adapters` projects | Recommended, consistent with EAD's modularity/testability goals |
| Team | Solo developer | Stakeholder — plan is a linear backlog, not parallel workstreams |
| Repo | Greenfield — new repository | Recommended; legacy is .NET Framework 4.8/WCF/OWIN and is not directly portable into ASP.NET Core 8 |
| CI/CD | GitHub Actions | **Assumed, not confirmed** — flag if this should be Azure DevOps, GitLab, or something else |
| Hosting | Own servers (self-hosted), Docker-based, cloud-agnostic | ADR-005, reconfirmed by stakeholder |
| Build timeline | Day 1 = full draft codebase. Hardening = separately paced, not compressed. | Stakeholder-agreed 2026-07-19 after timeline reality-check (see PROJECT_STATUS.md Change Log) |

---

## 4. Day 1 — Build Sprint

### Block 0 — Scaffolding (foundation; everything else depends on this)

| Task | Detail |
|---|---|
| Repo + solution | New git repo; `.sln` with `Api`, `Application`, `Domain`, `Infrastructure`, `Adapters.Hiso`, `Adapters.Karo`, `Adapters.Erms` projects |
| Docker | `Dockerfile` per service + `docker-compose.yml` for local dev; no cloud-vendor-specific service dependency (ADR-005) |
| Logging (folds in Phase 9) | Serilog structured logging; correlation-ID middleware; `/health` and `/health/ready` endpoints |
| CI | GitHub Actions workflow: build + unit test on push (assumption — confirm tool) |
| Secrets | Config via environment variables + a Key-Vault-shaped abstraction (`ISecretProvider`) so a real vault swaps in later without a rewrite — replaces the hardcoded `tcpepms*1` pattern found in all three legacy systems |

**Acceptance:** solution builds, containers start, `/health` returns 200, CI pipeline runs green on an empty commit.

### Block 1 — Security Core (folds in Phase 8 + SRS Phase A hard blockers)

| Task | Detail | Source |
|---|---|---|
| Auth | Microsoft Entra ID integration + compatibility adapter so HSS Portal / ERMS keep their current request shape | ADR-002 |
| Token scoping | Tokens scoped to one patient + encounter + practice; origin scope (HISO/KARO/ERMS) determined structurally by entry point, never a self-reported field | ADR-003 |
| Tenant routing | Tenant-registry DB; per-request lookup routes to the correct sharded physical DB server; RLS on any server hosting more than one practice | ADR-001 |
| HISO session handling | Keep existing session-GUID mechanism; add 12-hour expiry; log failed lookups as security events (currently silently swallowed) | ADR-004, ADR-007 |
| Rate limiting | Middleware, config-toggle default **off** | ADR-008 |
| Error handling | RFC 7807-inspired shape; `detail`/`errors[].message` always generic — full detail to structured logs only, correlated via `traceId` | Contract Design §10 |
| CORS | Narrowed from legacy's open configuration | SRS Phase A |
| SQL injection fix | Parameterize the confirmed injection in `DMSDA.cs`/`DBMessages.cs`; modules stay in the codebase, nothing deleted | Stakeholder decision 2026-07-19 |
| Input validation / output encoding | Applied globally via middleware/model validation, not per-endpoint ad hoc | SRS §12, OWASP Top 10 |

**Acceptance:** a request without a valid token is rejected; a valid HISO-scoped token cannot read KARO/ERMS data; the injection path returns a parameterized-query result, not raw concatenation.

### Block 2 — Domain Endpoints (build in this order)

All 18 groups follow the standing platform-wide rule: **do not remove or silently skip anything an old API implemented**, including previously-excluded dead/broken endpoints.

| # | Domain group | Notes |
|---|---|---|
| 1 | Patient Demographics | **3 separate legacy-shaped endpoints** (HISO `getData`, KARO `GetDemographics`, ERMS `GetPatientData`) — not merged; reconciliation deferred (open item, see §7) |
| 2 | Clinical Notes | — |
| 3 | Conditions / Diagnoses | — |
| 4 | Medications | — |
| 5 | Lab / Radiology Results | — |
| 6 | Documents / Attachments | Aspose-based rendering carried forward as-is, no replacement |
| 7 | Observations / Measurements | — |
| 8 | ACC45 Accident Claims (HISO-unique) | NZ HISO 10014.2 standard |
| 9 | Encounter Summary Templates (KARO-unique) | `GetEncounterSummary`/`SaveScreeningCode` built for real (working auth + persistence), not the legacy mock/no-op behavior |
| 10 | Tasks | — |
| 11 | Recalls (KARO-unique) | — |
| 12 | Screening | — |
| 13 | Providers / Practitioners | — |
| 14 | Practice / Session Context (COL/Pegasus) | Live consumer — 7 `COLController` endpoints including the financial `SaveInvoice` write |
| 15 | Billing / Invoicing | `SaveInvoice` field list undiscovered — build with known fields, backfill once source-checked (open item, see §7) |
| 16 | Tenant / Practice Administration | Internal, platform-admin scope only |
| 17 | Health / Diagnostics | — |
| 18 | HISO dead-code paths | `static mode`, `addInvoice`, `launchForm`, unreachable `saveProcessAction` — implemented for real, not excluded |

### Block 3 — Contract Verification

Re-validate `Unified-Healthcare-API_openapi.yaml` (v1.1.0, 42 endpoints) against what actually got built. Any drift gets corrected in the spec, not silently left mismatched.

**Day 1 exit criteria:** solution builds and runs in Docker; every endpoint in the OpenAPI spec has a real (not stubbed) handler; auth/tenant-routing/rate-limiting are wired end to end; SQL injection is fixed; CI is green. This is a *complete draft*, explicitly not yet verified, tested, or production-ready.

---

## 5. Post-Sprint — Hardening (separately paced)

| Stage | Duration (realistic) | Content |
|---|---|---|
| Automated test suite | 1–2 weeks | Unit + integration tests — none exist anywhere in the legacy estate today. Prioritize Phase A security paths first (auth bypass, injection fix, RLS isolation). |
| Security verification | 1 week | Confirm the SQL injection fix actually holds under adversarial input; confirm cross-tenant access is genuinely blocked; confirm secrets aren't exposed; review the 15 legacy ERMS connection strings are fully retired. |
| Load testing | 1 week | Toward the 10,000-concurrent-user target (SRS §5.5 — no prior SLA/latency figures exist, so this stage also has to define what "pass" means before it can be measured). |
| Staged rollout | Ongoing, self-paced | Config-toggle rollout per ADR-008; Zohaib owns the toggle switch-on decision, no fixed date tracked. |

**Do not skip straight to production after Day 1.** The build sprint proves the architecture works end to end; it does not prove it's safe to point at real PHI.

---

## 6. Risks

| Risk | Mitigation |
|---|---|
| Day-1 draft mistaken for production-ready | This document explicitly separates the two phases; hardening stage is a hard gate, not optional polish |
| No existing automated tests to regression-check against | Hardening stage's first task is building the suite before anything else in that stage proceeds |
| No confirmed SLA/latency figures for the 10,000-user target | Load-testing stage defines pass/fail thresholds as its first step, not an afterthought |
| KARO `SaveInvoice` field list still undiscovered | Built with currently-known fields; source/live-system check required before billing goes live (§7) |
| CI/CD tool assumed (GitHub Actions), not confirmed | Flag before Block 0 starts if incorrect |

---

## 7. Open Items Carried Into This Phase

| Item | Status |
|---|---|
| KARO `SaveInvoice` request field list | Undocumented — needs source/live-system check; once found, every field carries forward per the standing "don't remove anything" principle |
| Rate-limit thresholds | No source document specifies numbers — set conservatively at launch (config-toggle, default off per ADR-008), tightened once proven safe |
| Demographics field-by-field reconciliation | Deferred by stakeholder decision — 3 separate endpoints ship as-is; revisit later if/when sample responses are available |

---

## 8. Acceptance Criteria for This Phase

Phase 11 is complete when: this plan is agreed (done, 2026-07-19); Day 1 build sprint executes and meets the exit criteria in §4; hardening stage is scheduled with its own realistic timeline (§5); and PROJECT_STATUS.md reflects all of the above before Phase 12 (Development) formally starts.
