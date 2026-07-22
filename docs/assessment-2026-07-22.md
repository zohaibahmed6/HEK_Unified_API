# HEK Core API — Enterprise Assessment Report

**Date:** 2026-07-22 · **Author:** Claude (directed by Zohaib Ahmed) · **Baseline:** all four legacy compat modules complete and live-verified (HISO 6 ops, KARO 21, ERMS 22, COL 7)
**Governing spec:** `docs/HEK_UNIFIED_API_SPEC.md` · **Rule:** 100% backward compatibility — no changes to legacy endpoint URLs, models, status codes, or behavior.

## 1. Current Architecture Review
Clean Architecture is in place and healthy: `Domain` → `Application` (MediatR CQRS, FluentValidation pipeline, interface-only dependencies) → `Infrastructure` (legacy DAL ports, convention-scanned DI via `AddInfrastructureRepositories()`) → `Api` (thin controllers) plus per-system wire-format DTO projects (`Adapters.Hiso/Karo/Erms`, COL under `Adapters.Erms/Col`). Vertical-slice folders under `Api/Features/*`. Module isolation between HISO/KARO/ERMS/COL is enforced structurally (separate parsers, token validators, connection resolvers; zero shared session state).

**Strengths:** the spec's "each source system behind an adapter" pattern already exists and has been proven four times — onboarding a new legacy system is mechanical. Secrets flow through `ISecretProvider`; the options pattern is used throughout.

**Weaknesses:** the unified/canonical surface (17 modern REST controllers) is parked via `[NonController]`, so spec FR-3/4/5 (unified API, sparse fieldsets, field scoping) has no live surface yet; the canonical `AuthController` returns 501 (origin-scope open item); the tenant-registry-vs-numeric-PracticeID mismatch (HISO) remains unresolved.

## 2. Code Quality Assessment
Consistent conventions, sealed classes, async end-to-end, nullability enabled, zero build warnings. Legacy quirks are deliberately reproduced and documented at each site — correct for compatibility and must never be "cleaned up." Gaps: only 20 automated tests (crypto round-trip, ERMS mapper quirks, validation, auth scope) and none over the HTTP compat surfaces; MediatR handlers have no logging behavior. Duplication across the four modules is deliberate isolation and acceptable.

## 3. Security Review
**Present:** JWT bearer infrastructure (flag-disabled), rate limiting (flag-disabled), CORS allowlist, HTTPS redirect, a global exception handler that never leaks internals, parameterized SQL everywhere, gitignored secrets.
**Risks:** legacy compat endpoints are unauthenticated beyond their own legacy token schemes (by design — deploy behind network segmentation); the legacy Rijndael key is hardcoded (ported verbatim, required for compatibility — accepted risk); no security-headers middleware; no request-size limits on raw-body endpoints; no formal OWASP Top-10 pass yet.

## 4. Logging Review
Serilog console + rolling daily file (`src/Api/logs/`, 14-day retention), `CorrelationIdMiddleware`, exception logging via `GlobalExceptionHandler`. **Missing:** per-request/response logging (a successful call currently leaves no trace — verified), operation-level handler logging, database-operation logging, audit logging (spec FR-6), sensitive-data masking, centralized sink option.

## 5. Telemetry Review
**Missing entirely:** no OpenTelemetry, no traces or metrics, no duration/error-rate/success-rate measurements, no SQL dependency tracking. Present: health checks `/health` (self) and `/health/ready` (SQL) with JSON output — a good foundation. This is the largest single gap against spec NFR-5.

## 6. Performance Review
Async throughout; SqlClient connection pooling; no obvious hotspots. Safe, non-behavioral opportunities: cache `XmlSerializer` instances (currently constructed per request in the ERMS controller), cache secret lookups for connection strings, response compression. The reflection-based legacy mappers are quirk-faithful — do NOT optimize them (behavior risk).

## 7. Scalability Review
The API is stateless (legacy tokens live in the legacy DBs) and horizontally scalable as-is. In-memory idempotency/cache stores are per-instance (swap point exists behind `IIdempotencyStore`). A Dockerfile exists; missing: docker-compose, container health-check wiring, and scale-out documentation (NFR-2/3 partially met).

## 8. Documentation Review
Strong session history (`hek_analysis/PROJECT_STATUS.md` append-only log, `AI_USAGE_LOG.md`, ADRs, SRS, contract docs). **Gaps vs spec:** no `docs/datasets/{hiso,karo,erms}.md` field inventories, no `docs/datasets/unified-model.md`, no as-built `docs/architecture.md`, no documentation index, no deployment doc, no request-flow/sequence diagrams, no consumer-facing auth/gateway usage guide.

## 9. Risks
1. **Compat regression** — cross-cutting middleware touching response bodies could corrupt byte-exact legacy envelopes (utf-16 XML declarations, COL's broken-JSON sentinel, raw-text errors). Mitigation: golden-response regression suite BEFORE any pipeline additions (Phase 1).
2. **PHI in logs** — full request/response logging (explicitly chosen) writes patient data to disk; masking + retention strategy required.
3. **Spec tension** — unified canonical API (FR-3/4/5) vs the parked modern surface. Resolution: the hub surface will be a NEW versioned `/v1` module; parked controllers remain as reference; legacy surfaces untouched.
4. Pre-existing: HISO PracticeId-registry mismatch; AWS/Azure legacy branches deferred (documented).
5. Shared dev DB credentials in local config — a production secret store is required before real deployment.

## 10. Recommended Roadmap (phased, approval-gated; phases 1–6 have ZERO functional change)
| Phase | Content | Est. |
|---|---|---|
| 1 | Compat safety net — golden-response regression tests | 1–1.5 h |
| 2 | Observability core — request/response logging, operation logging, OpenTelemetry, masking | 1.5–2 h |
| 3 | Audit logging (FR-6) — consumer/timestamp/endpoint/outcome per call | ~1 h |
| 4 | Security hardening — headers, size limits, rate limiter, OWASP checklist | 1–1.5 h |
| 5 | Hub packaging — per-module DI extensions, docker-compose, onboarding guide | ~1.5 h |
| 6 | Documentation set — architecture, datasets, unified model, diagrams, index | 2–3 h |
| 7 | Unified canonical `/v1` API — sparse fieldsets, field-scope profiles, OAuth2, simulators (additive only) | 6–10 h |

**Approved session scope (2026-07-22): Phases 1–3.** Each subsequent phase requires explicit approval. The Phase 1 suite is re-run after every phase, forever.
