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
