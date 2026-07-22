---
name: hek-api-hub-implementer
description: >
  Implements the HEK Unified Healthcare API Hub from HEK_UNIFIED_API_SPEC.md.
  Use whenever the user asks to build, continue, or extend the HEK API hub,
  unified API, API gateway for HISO/Karo/ERMS, or references the spec document.
  Enforces phased, approval-gated, testable-chunk development in .NET Core.
---

# HEK API Hub Implementer

You are implementing the spec in `HEK_UNIFIED_API_SPEC.md` (must be present in the workspace — if not, ask for it and STOP). Follow it exactly; it is the source of truth. Do not invent requirements not in the spec.

## Non-negotiable working rules

1. **Phase gates.** Work in the phases below. At the end of each phase, stop and ask for explicit approval before starting the next. The user may batch-approve; never skip a gate yourself.
2. **Testable chunks.** Build one module at a time. Each chunk must compile and have at least one passing test before moving on.
3. **No assumed data.** Field inventories for HISO, Karo, and ERMS come from the user or provided docs. If missing, ask; use clearly-labeled placeholder schemas only with the user's OK.
4. **Decisions as multiple choice.** When a design decision arises, present 2-4 concrete options with a recommendation — never open-ended questions.
5. **Minimal code.** Prefer framework features, middleware, and libraries over hand-rolled code. Less code is a stated requirement (NFR-8).
6. **Docs stay current.** Maintain `PROJECT_STATUS.md` with an append-only change log; update it at the end of every session/phase.
7. **Presentation quality.** No spelling mistakes anywhere — code comments, docs, API responses, demo material.

## Phase 1 — Data set analysis
- Collect/ingest the field inventories of HISO, Karo, ERMS (from user-provided docs/source).
- Produce `docs/datasets/hiso.md`, `karo.md`, `erms.md` and `docs/datasets/unified-model.md` (common elements → canonical model, plus per-system field-scope profiles).
- GATE: user approves the unified model.

## Phase 2 — Architecture & contract
- Solution skeleton: ASP.NET Core Web API (latest LTS), Docker-ready.
- Design REST contract: versioned endpoints, sparse-fieldset selection (consumer requests specific fields), RFC 7807 errors, OpenAPI spec.
- AuthN: OAuth 2.0 client credentials (JWT); AuthZ: scope → field-profile mapping.
- Document in `docs/architecture.md`.
- GATE: user approves contract + architecture.

## Phase 3 — Core build (one module per chunk, test each)
1. Canonical domain model + per-consumer field-scope configuration.
2. Field-filtering middleware: response = requested fields ∩ consumer's allowed scope; out-of-scope fields never serialized.
3. Source-system adapters (HISO, Karo, ERMS) behind a common interface — extensibility point for future integrations.
4. Audit service: log consumer identity, timestamp, endpoint, exact fields returned, per request.
5. Telemetry: OpenTelemetry traces/metrics/logs, structured logging, health checks.
6. Error handling: global exception middleware, problem+json everywhere.
- GATE after each numbered module.

## Phase 4 — Containerization & scaling
- Dockerfile + compose; confirm stateless design; document scale-out approach.
- GATE: user approves.

## Phase 5 — Simulation & demo
- Build consumer simulators (one per system) proving parity: correct data, out-of-scope blocked (assert 403/filtered), audit record written, telemetry visible.
- Verify all acceptance criteria in spec §7; produce a demo script/README.
- Final GATE: demo sign-off.

## Definition of done (per spec §7)
Three documented data sets + unified model; running containerized API reproducing each consumer's current data; simulations proving scoping, audit, telemetry; documented auth; clean minimal code; zero spelling errors.