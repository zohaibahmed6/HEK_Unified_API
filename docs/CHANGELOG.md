# Changelog

Per-change log — more granular than `hek_analysis/PROJECT_STATUS.md`'s per-session entries. Append
one entry here after every approved change (per the documentation skill's Rule 4 / Change
Tracking). This is a procedural mechanism, not automation — there is no hook/cron wiring this file;
each session is responsible for adding its own entry before/at the end of the session.

**Entry format:**
```
## YYYY-MM-DD — <short title>
- Feature: <what changed>
- Reason: <why>
- Files changed: <paths>
- Documentation updated: <doc paths touched>
- Breaking changes: <none | description>
```

---

## 2026-07-31 (2) — Built complete verified Postman collection (Legacy vs New API, all 4 systems)
- Feature: added `crosscheck/HEK_Complete_Verified.postman_collection.json` (118 requests across
  "Legacy - HISO/ERMS/KARO/COL" and "New API - HISO/ERMS/KARO/COL" folders) and updated
  `crosscheck/HEK_4APIs.postman_environment.json` with base URLs/credentials/IDs for all 4 systems.
- Reason: prior crosscheck attempt (`crosscheck/HekCoreApi-Crosscheck.postman_collection.json`) used
  guessed/incomplete sample data; this pass re-derived every request's body/headers/params from real
  evidence (`crosscheck/errors.md`, `crosscheck/SUMMARY.md`, `crosscheck/PARITY_MEMORY.md`, and the
  already-verified `HEK_4APIs_Manual_Verification.postman_collection.json`) and cited the source in
  each request's `description` field, so the collection itself is traceable and no field is invented.
- Files changed: `crosscheck/HEK_Complete_Verified.postman_collection.json` (new),
  `crosscheck/HEK_4APIs.postman_environment.json` (updated), `docs/PROJECT_MASTER.md` §2.
- Documentation updated: `docs/PROJECT_MASTER.md` §2 (this entry).
- Breaking changes: none (docs/tooling only, no `src/` changes).
- Known gaps: HISO `getData`/`saveContainer`/`getFormView` and ERMS `GetRadiologyReportDetails` have
  no full real success body preserved verbatim anywhere in the repo's docs - marked in-collection as
  needing a live capture rather than fabricated.

## 2026-07-23 (3) — Implemented full OpenTelemetry telemetry (closed NFR-5 gap)
- Feature: added OpenTelemetry tracing + metrics to `src/Api/Program.cs` (ASP.NET Core, HTTP client,
  and SQL Server dependency instrumentation, plus .NET runtime metrics), exported via OTLP to a new
  `aspire-dashboard` container in `docker-compose.yml` (falls back to console output for plain
  `dotnet run`). Added a custom `HekTelemetry` meter (`src/Api/Telemetry/HekTelemetry.cs`) recording
  FR-5/FR-6 field-scoping counters (fields returned vs. blocked per call), wired into
  `CanonicalDemographicsController` as the first of the 15 canonical controllers.
- Reason: `docs/assessment-2026-07-22.md` §5 flagged telemetry as "missing entirely," the single
  largest gap against spec NFR-5. User explicitly asked for complete telemetry ahead of the demo.
- Files changed: `Directory.Packages.props`, `src/Api/HekCoreApi.Api.csproj` (added OpenTelemetry
  packages + narrow `NoWarn` for a persistent transitive-dependency advisory, NU1902/GHSA-4625-4j76-fww9,
  confirmed unaffected by version bumps 1.9.0→1.11.1), `src/Api/Program.cs`, new
  `src/Api/Telemetry/HekTelemetry.cs`, `src/Api/Features/Canonical/Controllers/CanonicalDemographicsController.cs`,
  `docker-compose.yml` (new `aspire-dashboard` service), `src/Api/appsettings.Development.json`
  (documented `Otel:OtlpEndpoint` setting).
- Documentation updated: `docs/architecture.md` (new Telemetry section), `docs/deployment.md` (new
  Telemetry section + compose service description), `docs/assessment-2026-07-22.md` §5 and roadmap
  table, `docs/DOCUMENT_INDEX.md`.
- Breaking changes: none. Verified: full solution build succeeds, all 23 tests still pass, and the
  app was smoke-tested live under `dotnet run` — boots cleanly, `/health` returns 200, console
  exporter confirmed emitting real OpenTelemetry data.
- Not done in this pass (flagged, not silently skipped): the `hek.canonical.fields_*` counters are
  only wired into `CanonicalDemographicsController`; replicating the same call to the other 14
  canonical controllers is a fast, mechanical follow-up, not attempted here given the demo timeline.

## 2026-07-23 (2) — Resolved the /v1 versioning-policy inconsistency
- Feature: `Unified-Healthcare-API_API-Contract-Design.md` Section 9 now documents a two-tier
  versioning model instead of a single "no version anywhere" policy: legacy-compat endpoints
  (HISO/KARO/ERMS/COL translators) stay unversioned per the existing zero-consumer-change ADRs;
  the canonical hub surface (all 15 `Canonical*Controller`s) uses `/v1` URL-path versioning,
  confirmed deliberate and uniform across all 15, not a one-off.
- Reason: the same-day demographics sync surfaced that the new merged demographics endpoint's
  `/v1` prefix looked inconsistent with Section 9's original no-versioning policy. Checking the
  other 14 canonical controllers showed the `/v1` prefix is universal, so the real fix was
  updating the outdated policy text (written 2026-07-19, before the canonical hub existed) rather
  than stripping `/v1` from working code. URL-path versioning also directly satisfies spec NFR-9
  ("research-grounded design... following Azure/AWS API gateway patterns").
- Files changed: `hek_analysis/docs/architecture/Unified-Healthcare-API_API-Contract-Design.md`
  (Section 9 resolution + §11 Versioning Safety risk row + revision history v1.4),
  `hek_analysis/docs/architecture/Unified-Healthcare-API_openapi.yaml` (1.1.5 changelog note
  updated from "flagged, not resolved" to resolved).
- Documentation updated: `docs/DOCUMENT_INDEX.md`.
- Breaking changes: none — documentation-only, no controller routes were touched.

## 2026-07-23 — Synced demographics contract docs to real code (closed a live doc/code drift)
- Feature: `hek_analysis/docs/architecture/Unified-Healthcare-API_API-Contract-Design.md` and its
  companion `Unified-Healthcare-API_openapi.yaml` (bumped 1.1.4 → 1.1.5) now document the real
  `GET /v1/patients/{patientId}/demographics` merged endpoint (`CanonicalDemographicsController`)
  instead of the four separate per-legacy-system endpoints they previously specified.
- Reason: a prior session flagged this as a real drift — the contract doc/spec still described 4
  endpoints while the actual code has one merged, field-scoped endpoint — worth fixing before
  anyone reviews the contract doc against a live demo.
- Files changed: `hek_analysis/docs/architecture/Unified-Healthcare-API_API-Contract-Design.md`
  (Sections: revision history, alignment table, 4.2, 6.2, Decision 6 — old content collapsed into
  labeled historical notes, not deleted), `hek_analysis/docs/architecture/Unified-Healthcare-API_openapi.yaml`
  (new `/v1/patients/{patientId}/demographics` path + `DemographicsCanonical` schema; old 4 paths
  marked `deprecated: true` and kept for traceability).
- Documentation updated: `docs/DOCUMENT_INDEX.md` (API Contract section + Pre-Spec section rows).
- Breaking changes: none to running code (docs-only change). Flagged, not fixed: the new path's
  `/v1` prefix is itself a minor departure from the contract doc's own "no version in URL" policy
  (Section 9) — noted in the yaml changelog as a known, unresolved inconsistency.

## 2026-07-22 — Initial pragmatic documentation set created
- Feature: created the documentation subset identified as missing in
  `docs/assessment-2026-07-22.md` §8 (no time for the skill's full ~25-file template before a
  same-day demo).
- Reason: close the doc gaps against `docs/HEK_UNIFIED_API_SPEC.md` (navigation index, as-built
  architecture, per-system field inventories, deployment doc, auth guide, changelog mechanism).
- Files changed (created): `docs/DOCUMENT_INDEX.md`, `docs/architecture.md`,
  `docs/datasets/hiso.md`, `docs/datasets/karo.md`, `docs/datasets/erms.md`,
  `docs/datasets/unified-model.md`, `docs/deployment.md`, `docs/auth-guide.md`,
  `docs/CHANGELOG.md` (this file).
- Documentation updated: all of the above are new; no existing docs were edited except this
  changelog going forward.
- Breaking changes: none — documentation only, no code changed.
- Drift noted (not fixed, per instructions): the OpenAPI yaml
  (`hek_analysis/docs/architecture/Unified-Healthcare-API_openapi.yaml`, v1.1.4) documents four
  separate demographics paths (`/patients/{patientId}/demographics/{hiso,karo,erms,col}`), but the
  real `CanonicalDemographicsController` implements one merged, versioned endpoint
  (`v1/patients/{patientId}/demographics`) that infers the source system from the JWT's
  `OriginScope` claim and accepts `?fields=` for sparse fieldsets. This is a structural difference,
  not a trivially-missing route, so the yaml was left as-is; see `docs/architecture.md` for detail.
