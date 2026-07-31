# Documentation Index

**Start here:** `docs/PROJECT_MASTER.md` is the single source of truth for "what's
actually done" and "what's open" — read it before this index.

**API contract source of truth:** `hek_analysis/docs/architecture/Unified-Healthcare-API_openapi.yaml`
(OpenAPI 3.0.3, version `1.1.4`, companion design rationale in
`hek_analysis/docs/architecture/Unified-Healthcare-API_API-Contract-Design.md`), verified live via
Swashbuckle at `/swagger/v1/swagger.json` on the running API. Treat any other route list (including
this index) as secondary to those two files.

## Spec & Assessment
| Doc | Description |
|---|---|
| `docs/HEK_UNIFIED_API_SPEC.md` | Governing product spec: FR-1..9, NFR-1..9 for the unified API hub. |
| `docs/assessment-2026-07-22.md` | Enterprise self-audit (architecture, code quality, security, logging, telemetry, performance, scalability, docs, risks, roadmap). **§5 Telemetry updated 2026-07-23: NFR-5 gap resolved**, see below. |
| `docs/CHANGELOG.md` | Per-change log, going forward (see Rule 4 of the documentation skill). |

## API Contract
| Doc | Description |
|---|---|
| `hek_analysis/docs/architecture/Unified-Healthcare-API_openapi.yaml` | **Source of truth**, v1.1.5. Includes one merged `GET /v1/patients/{patientId}/demographics` (synced 2026-07-23 to `CanonicalDemographicsController`); the 4 legacy per-system demographics paths are kept `deprecated: true` for historical traceability, not implemented by any real controller. |
| `hek_analysis/docs/architecture/Unified-Healthcare-API_API-Contract-Design.md` | Rationale, traceability, and reconciliation decisions behind the yaml. v1.3 — demographics section synced to real code (see below). |

## Architecture
| Doc | Description |
|---|---|
| `docs/architecture.md` | As-built layer breakdown, CQRS/MediatR pattern, legacy-compat isolation, request-flow example. Now includes an OpenTelemetry section (added 2026-07-23) and the resolved yaml/code demographics drift. |
| `docs/adr/ADR-012-solution-structure-and-block1-inferences.md` | ADR: solution structure, one-Dockerfile decision, Block 1 inferences. |
| `docs/adr/Unified API Hub Specification.md` | Superseded/duplicate copy of the unified spec kept under adr/ — `docs/HEK_UNIFIED_API_SPEC.md` is canonical. |

## Pre-Spec Technical Documentation (now spec-cross-referenced, 2026-07-22)
Older docs derived bottom-up from reverse-engineering HISO/Karo/ERMS (2026-07-18/19), predating
`docs/HEK_UNIFIED_API_SPEC.md`. Each now carries an "Alignment with HEK_UNIFIED_API_SPEC.md"
section mapping FR-1..9/NFR-1..9 to where it's satisfied, partial, or a gap — see each doc for detail.
| Doc | Description |
|---|---|
| `hek_analysis/docs/SRS_UnifiedHealthcareAPI.md` | SRS v1.0 (now v0.2 w/ alignment section). Gaps flagged: FR-4/FR-9/NFR-9. |
| `hek_analysis/docs/architecture/Unified-Healthcare-API_EAD.md` | Enterprise Architecture Document (now v1.1 w/ alignment section). Gaps flagged: FR-9/NFR-5(doc-local)/NFR-9. **Note:** a byte-identical duplicate exists at `hek_analysis/docs/Unified-Healthcare-API_EAD.md` — only the `architecture/` copy was edited; the duplicate was left as-is per instructions. |
| `hek_analysis/docs/architecture/Unified-Healthcare-API_ADRs.md` | ADR log (now v1.1 w/ alignment section). Gaps flagged: FR-6/FR-9/NFR-9. |
| `hek_analysis/docs/architecture/Unified-Healthcare-API_API-Contract-Design.md` | API contract rationale (now v1.4). **Resolved 2026-07-23:** demographics section (4.2/6.2) and the openapi.yaml itself were both synced to match real code (`CanonicalDemographicsController` — one merged `GET /v1/patients/{patientId}/demographics`, `?fields=` scoping). Old 4-endpoint design kept as a collapsed historical note, not deleted. **Also resolved 2026-07-23:** the `/v1` prefix inconsistency — confirmed all 15 canonical hub controllers use it deliberately; Section 9 now documents a two-tier versioning model (legacy-compat unversioned, hub URL-versioned). |
| `hek_analysis/docs/Unified-Healthcare-API_ImplementationPlan.md` | Implementation plan (now v1.1 w/ alignment section). Gaps flagged: FR-9/NFR-9; FR-6/NFR-5 partial. |

## Datasets (field inventories)
| Doc | Description |
|---|---|
| `docs/datasets/hiso.md` | HISO field inventory, derived from `src/Adapters.Hiso/*` and the concept-dictionary pipeline. |
| `docs/datasets/karo.md` | KARO/HSS field inventory, derived from `src/Adapters.Karo/*`. |
| `docs/datasets/erms.md` | ERMS (+ COL/Pegasus) field inventory, derived from `src/Adapters.Erms/*`. |
| `docs/datasets/unified-model.md` | Best-effort intersection/union across the three systems (draft, not signed off). |

## Deployment / Operations
| Doc | Description |
|---|---|
| `docs/deployment.md` | Dockerfile, docker-compose status (now includes `aspire-dashboard` telemetry viewer), secrets, env vars, health endpoints, known container-networking blocker. |

## Auth
| Doc | Description |
|---|---|
| `docs/auth-guide.md` | How each legacy compat surface authenticates today, plus the canonical JWT/origin-scope design and its open item. |

## Session History / Decisions (append-only — do not duplicate, link only)
| Doc | Description |
|---|---|
| `hek_analysis/PROJECT_STATUS.md` | Append-only session log; numbered open items referenced throughout these docs. |
| `hek_analysis/HANDOFF_TO_FABLE.md` | Handoff notes between sessions/agents. |

## Demo
| Doc | Description |
|---|---|
| `docs/demo/CanonicalDemoScript.md` | Script for demoing the canonical `/v1` surface. |
