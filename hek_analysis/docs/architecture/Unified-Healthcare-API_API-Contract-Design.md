# API Contract Design — Unified Healthcare API

## 1. Document Control

| Field | Value |
|---|---|
| Title | API Contract Design Document — Unified Healthcare API (consolidating HISO, KARO, ERMS) |
| Version | 1.0 (Draft for stakeholder review) |
| Date | 2026-07-19 |
| Author | API Contract Designer skill, from validated SRS + Enterprise Architecture inputs |
| Status | Draft — ready for Implementation Planning pending resolution of Section 18 Open Questions |
| Source Documents | `docs/SRS_UnifiedHealthcareAPI.md` v1.0; `docs/architecture/Unified-Healthcare-API_EAD.md` (28 sections, Review Board 8 PASS/4 WARNING/0 FAIL); `docs/architecture/Unified-Healthcare-API_ADRs.md` (ADR-001–011 + follow-up decision log); `docs/analysis/ComparisonReport.md`; `docs/analysis/MigrationRecommendations.md`; `docs/analysis/{hiso,karo,erms}/EndpointInventory.md`, `BusinessRules.md`; `PROJECT_STATUS.md` |

### Revision History

| Version | Date | Description |
|---|---|---|
| 1.0 | 2026-07-19 | Initial contract design, Phase 7 of the project lifecycle, following the completed (Phase 6 skipped by stakeholder decision) Enterprise Architecture phase |
| 1.1 | 2026-07-19 | Stakeholder Q&A round: demographics kept separate per legacy system instead of merged (Section 4.2, 6.2); HISO's dead-code actions (`addInvoice`, `launchForm`, static mode) implemented rather than excluded (Section 4.9); KARO's `GetEncounterSummary`/`SaveScreeningCode` implemented for real rather than excluded (Section 4.10, 4.13); rate limiting confirmed as a built, config-toggled capability (Section 14); COL/Pegasus confirmed live, Azure mirror confirmed not live but retained (Section 16, 18). See Section 8, Decisions 4–6, and `PROJECT_STATUS.md` for full traceability of each stakeholder answer. |
| 1.2 | 2026-07-22 | Added spec-alignment cross-reference; see PROJECT_STATUS.md / DOCUMENT_INDEX.md |
| 1.3 | 2026-07-23 | **Synced Section 4.2 and Section 6.2 to match real running code**: the four-separate-demographics-endpoints design (v1.1) is superseded by the actual `CanonicalDemographicsController` (`src/Api/Features/Canonical/Controllers/CanonicalDemographicsController.cs`) — one merged endpoint, `?fields=` sparse-fieldset support, and server-side per-origin field scoping via `FieldSelector`. v1.1's four-endpoint tables/examples are kept below, labeled superseded, for historical traceability rather than deleted. |
| 1.4 | 2026-07-23 | **Resolved the `/v1` URL-prefix inconsistency** flagged when v1.3 synced demographics: confirmed all 15 canonical hub controllers use a `v1/` route prefix (deliberate, uniform — not a one-off). Section 9 revised to a two-tier versioning model: legacy-compat endpoints stay unversioned (unchanged, ADR-002/004/007), the canonical hub uses URL-path versioning (`/v1`, `/v2` for future breaking changes) per spec NFR-9's Azure/AWS-gateway-pattern guidance. §11 Versioning Safety risk row updated to match. |

### Alignment with HEK_UNIFIED_API_SPEC.md (2026-07-22)

**Resolved 2026-07-23:** the drift flagged below (four separate demographics endpoints vs. the spec's unified-model requirement) has been synced — Section 4.2 and Section 6.2 now document the real merged `GET /v1/patients/{patientId}/demographics` endpoint, matching `CanonicalDemographicsController`. This document's v1.1 "kept separate" stakeholder decision (Section 8, Decision 6) is now superseded by that later implementation choice — see Section 4.2's superseded note for the full history rather than erasing the original decision record.

| Spec Requirement | Where addressed in this doc | Status |
|---|---|---|
| FR-2 (unified/canonical model) | §4.2 now documents one merged endpoint, matching real code | **Aligned (as of v1.3, 2026-07-23)** |
| FR-3 (single REST API) | §2 "single platform," §4 endpoint inventory | Aligned overall |
| FR-4 (return only requested fields) | §4.2/§6.2 now document the real `?fields=` sparse-fieldset convention (`FieldSelector.Project`) | **Aligned (as of v1.3, 2026-07-23)** |
| FR-5 (per-consumer field scoping) | §4.2 now documents the real per-`originScope` allowed-field lists enforced server-side (`AllowedFieldsByOrigin` in `CanonicalDemographicsController`), in addition to §3.3/§3.4's resource-scoped authorization (ADR-003) | **Aligned (as of v1.3, 2026-07-23)** for demographics; other Section 4 resources still use the pre-spec per-legacy-system pattern and haven't been individually re-synced |
| FR-6 (audit: exact fields) | §4.2 now notes the real `CanonicalDemographicsAccess` structured log line (consumer, practiceId, patientId, fields returned) | Partial — real log-line audit exists for this endpoint; still not a durable/queryable audit store (see `docs/assessment-2026-07-22.md` §4) |
| FR-9 (simulation) | Not addressed in this document — predates demo directive; see `docs/demo/CanonicalDemoScript.md` instead | Gap (in this doc only; met elsewhere) |
| NFR-7 (systematic error handling) | §3 point 7, "meaningful HTTP status semantics," FR-HTTP-01 | Aligned |
| NFR-9 (Azure/AWS gateway research) | Not addressed | Gap |

---

## 2. Executive Summary

This document defines the request/response contract, endpoint inventory, versioning, error, pagination, idempotency, and authentication conventions for the **Unified Healthcare API** — the single platform replacing HISO, KARO, and ERMS per the approved SRS and Enterprise Architecture Document (EAD).

**Mode: Migration/consolidation** (see Section 5). This contract reconciles three legacy wire formats (HISO SOAP/XML, KARO JSON, ERMS XML + ERMS/COL JSON) into one canonical internal REST/JSON contract, while preserving every existing external consumer's exact current request/response shape through edge compatibility adapters — consistent with the "zero consumer-side change" principle already established for authentication (ADR-002), HISO sessions (ADR-004), and HISO routing (ADR-007).

This document hands off to **Phase 11, Implementation Planning**. No further architecture-level decisions should be required to begin building against this contract — the remaining open items in Section 18 are business/data confirmations, not contract-shape decisions.

---

## 3. Contract Design Principles

Pulled directly from the SRS and EAD/ADRs, not invented here:

1. **One canonical implementation per capability** (SRS §6.1, ComparisonReport §11) — the near-total duplication between KARO's and ERMS's read surfaces collapses into one endpoint per capability.
2. **Zero consumer-side change wherever the architecture already committed to it** (ADR-002 for HSS Portal/ERMS auth, ADR-004/ADR-007 for HISO) — the contract's edge adapters must reproduce each legacy consumer's exact existing wire shape; only the platform's *internal* canonical contract is new.
3. **Resource-scoped authorization** (ADR-003) — every token is valid for one patient + one encounter + one practice; the contract must make this scope explicit in every protected endpoint, not implicit.
4. **Structurally-determined origin scope, never caller-supplied** (ADR-003) — no request parameter may declare which legacy system it originates from; origin is derived from which credential/entry-point authenticated the call.
5. **Config-toggleable rollout, including security** (ADR-008) — the contract itself does not encode toggle state, but every new-behavior endpoint (real auth, origin-scoping, idempotency contract) must be safe to run in a "legacy-equivalent" mode during rollout.
6. **No database schema changes** (ADR-010) — the contract's data shapes are constrained to fields obtainable from the existing Indici PMS schema/stored procedures; no field is invented that isn't traceable to a legacy response or a stated requirement.
7. **Meaningful HTTP status semantics** (FR-HTTP-01) — replacing KARO/ERMS's confirmed "always HTTP 200" anti-pattern is a hard requirement of this contract, not a nice-to-have.
8. **Documented idempotency, not magic numbers** (FR-IDEM-01) — every write endpoint that legacy handled via an undocumented magic return code (`-3`, `-5`) gets an explicit, testable contract behavior instead.

---

## 4. Resource & Endpoint Inventory

All canonical endpoints below are REST/JSON, served under a single unversioned base path (see Section 9 for the versioning rationale). Legacy wire-format compatibility (HISO SOAP, ERMS `APIController` XML) is preserved by edge adapters that translate to/from this canonical contract — see Section 6 and Section 8, Reconciliation Decision #1.

### 4.1 Authentication

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/auth/token` | POST | Canonical token issuance. Edge adapters accept each legacy consumer's exact existing `Authenticate` payload shape (HSS Portal's GET/POST query+JSON form, ERMS's XML `Credential`, COL's JSON `Credential`) and translate to this call internally. | HISO: none (session-based, see 4.9); KARO `Authenticate` (GET+POST); ERMS `APIController.Authenticate`, `COLController.Authenticate` | FR-AUTH-01/03, ADR-002, ADR-003 |

### 4.2 Patient Demographics

**Superseded 2026-07-23 — synced to real code.** v1.1 (2026-07-19, Section 8 Decision 6) specified four separate, legacy-shaped endpoints instead of one merged shape, since no sample live responses or time were available to do a field-by-field reconciliation. The actual implementation (`CanonicalDemographicsController`, `src/Api/Features/Canonical/Controllers/`) took a different, later approach that resolves the same underlying concern without needing that reconciliation up front: **one merged endpoint**, where each caller's `originScope` (ADR-003) determines both which legacy repository call is made server-side *and* which canonical fields that caller is allowed to see — so HISO/KARO/ERMS/COL responses never need a shared field union, each caller still only ever sees its own system's fields, just via one URL instead of four.

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/v1/patients/{patientId}/demographics` | GET | One canonical demographics endpoint for every consumer. Server resolves the caller's `originScope` from its token, fetches from the matching legacy repository (`IDemographicsRepository.Get{Hiso,Karo,Erms,Col}Async`), maps onto a shared `DemographicsCanonical` shape (unproduced fields null), then narrows the response to that origin's allowed-field list intersected with an optional `?fields=` query param (FR-4/FR-5). Every call logs a `CanonicalDemographicsAccess` line (consumer, practiceId, patientId, exact fields returned — FR-6). | HISO `getData`; KARO `GetDemographics`; ERMS `GetPatientData`; COL `GetCurrentPatientData` | FR-PAT-01, FR-2, FR-3, FR-4, FR-5, FR-6 |

Per-origin allowed fields (server-enforced, not caller-configurable):

| Origin | Allowed canonical fields |
|---|---|
| Hiso | `patientId`, `practiceId`, `firstName`, `lastName`, `dateOfBirth` |
| Karo | `patientId`, `practiceId`, `firstName`, `lastName`, `dateOfBirth`, `dateOfEnrolment`, `endEnrolmentDate` |
| Erms | `patientId`, `encounterId`, `firstName`, `lastName`, `dateOfBirth`, `nhi` |
| Col | `patientId`, `practiceId`, `firstName`, `lastName` |

<details>
<summary>Historical record — v1.1's four-separate-endpoint design (2026-07-19, superseded 2026-07-23, kept for traceability, not current)</summary>

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/patients/{patientId}/demographics/hiso` | GET | HISO-shaped demographic data (concept-driven fields) | HISO `getData` | FR-PAT-01 |
| `/patients/{patientId}/demographics/karo` | GET | KARO-shaped demographic data (`DemographicInfo` fields) | KARO `GetDemographics` | FR-PAT-01 |
| `/patients/{patientId}/demographics/erms` | GET | ERMS-shaped demographic data (`PatientData` fields) | ERMS `GetPatientData` | FR-PAT-01 |
| `/patients/{patientId}/demographics/col` | GET | COL/Pegasus-shaped demographic data (`PegasusAPIModel.PatientData` fields) | COL `GetCurrentPatientData` | FR-PAT-01 |

</details>

### 4.3 Clinical Notes

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/patients/{patientId}/encounters/{encounterId}/notes` | GET | Retrieve clinical/consult notes; `sinceDate`/`untilDate` filters, default 24-month lookback if omitted | HISO `getData`; KARO `GetClinicalNotes`; ERMS `GetConsultNotes` | FR-CLIN-01, FR-CLIN-02 (ERMS-BR-05) |
| `/patients/{patientId}/encounters/{encounterId}/notes` | POST | Save a clinical/consult note | KARO `SaveClinicalNotes` | FR-CLIN-01 |

### 4.4 Conditions / Diagnoses

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/patients/{patientId}/encounters/{encounterId}/conditions` | GET | Retrieve conditions/diagnoses | HISO `getData`, ACC45 diagnosis builders; KARO `GetConditions`; ERMS `GetClassifications`; COL `GetDiagnosisData` | FR-COND-01 |
| `/patients/{patientId}/encounters/{encounterId}/conditions` | POST | Save a condition/diagnosis; duplicate submission is a documented non-error (see Section 12) | KARO `SaveCondition` (KARO-BR-12, KARO-BR-21) | FR-COND-01, FR-COND-02, FR-IDEM-01 |

### 4.5 Medications

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/patients/{patientId}/encounters/{encounterId}/medications` | GET | Retrieve medications; `view=regular\|prescribed` query param makes ERMS's boolean-flag distinction explicit in the contract | HISO `getData`; KARO `GetMedications`; ERMS `GetPrescribedMedications`/`GetRegularMedications` (ERMS-BR-11) | FR-MED-01 |

### 4.6 Lab / Radiology Results

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/patients/{patientId}/encounters/{encounterId}/lab-results` | GET | List lab results, `sortOrder`/date-range filters (ERMS-BR-12) | HISO `getData`; KARO `GetLabResults`; ERMS `GetLaboratoryReportList` | FR-LAB-01 |
| `/patients/{patientId}/encounters/{encounterId}/lab-results/{reportId}` | GET | Lab report detail/content | ERMS `GetLaboratoryReportDetails` (ERMS-BR-13, RTF transcoding) | FR-LAB-01 |
| `/patients/{patientId}/encounters/{encounterId}/radiology-results` | GET | List radiology results | ERMS `GetRadiologyReportList` | FR-LAB-01 |
| `/patients/{patientId}/encounters/{encounterId}/radiology-results/{reportId}` | GET | Radiology report detail/content | ERMS `GetRadiologyReportDetails` | FR-LAB-01 |

### 4.7 Documents / Attachments

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/patients/{patientId}/documents` | GET | List documents; `direction=in\|out`, `contentType`, `referenceId`, `sortOrder`, `subject`, date-range filters | KARO `GetDocuments`; ERMS `GetScannedList`, `GetDischargeSummaryReportList` | FR-DOC-01, FR-DOC-02, FR-DOC-04 |
| `/patients/{patientId}/documents/{documentId}` | GET | Retrieve a document's content (base64) | HISO `getFormView`; KARO `GetPatientAttachment`; ERMS `GetScannedDetails`, `GetDischargeSummaryDetails` | FR-DOC-01, FR-DOC-04 |
| `/patients/{patientId}/documents` | POST | Save a document; `direction` (in/out) and `contentType`/MIME both required — see Section 8, Reconciliation Decision #2 | HISO `saveContainer`; KARO `SaveDocument`; ERMS `SaveDocument` | FR-DOC-01, FR-DOC-02, FR-DOC-03 |

### 4.8 Observations / Measurements

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/patients/{patientId}/encounters/{encounterId}/observations` | GET | Retrieve observations/measurements; optional `conceptId` filter | HISO `getData`; KARO `GetObservations`; ERMS `GetPatientMeasurement` | FR-COND-01 (functional grouping) |
| `/patients/{patientId}/encounters/{encounterId}/observations` | POST | Save observations; at least one of nine measurement fields required (KARO-BR-14) | KARO `SaveObservations` | — |

### 4.9 ACC45 Accident Claims (HISO-unique)

HISO's existing `FormSessionService.svc` SOAP contract and its `SessionGUID` mechanism are preserved unchanged for the HealthLink-style form engine per ADR-004/ADR-007 — no consumer-side change. The endpoints below are the **canonical internal REST equivalents** the SOAP edge adapter calls; they are not directly exposed to HISO's existing consumer.

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/acc45/sessions/{sessionKey}` | GET | Resolve session GUID to Provider/Patient/Appointment/Practice context | HISO `GetSession`/`HealthLinkSession.GetByGUID` (HISO-BR-01/02) | FR-BILL-02 |
| `/acc45/sessions/{sessionKey}/forms` | GET | Retrieve form data via concept-mapping (HISO-BR-03/08/09/10); `mode=dynamic\|static` reflects the practice's `IsDynamic` setting. Legacy `static` mode was an empty, unimplemented stub (HISO-BR-17) — per stakeholder decision (2026-07-19, Section 8 Decision 4), this is built out as real functionality rather than carried forward as a silent no-op. | HISO `getData` | FR-BILL-02 |
| `/acc45/sessions/{sessionKey}/forms/{formInstanceId}` | PUT | Save completed form: render view to DMS, then persist ACC45 definition/diagnosis/referral, DMS GUID linked (HISO-BR-12 sequencing) | HISO `saveContainer` | FR-BILL-02 |
| `/acc45/sessions/{sessionKey}/forms/{formInstanceId}` | GET | Retrieve a previously saved form's rendered view/definition | HISO `getFormView` | FR-BILL-02 |
| `/acc45/sessions/{sessionKey}/actions` | POST | Dispatch a form action: `save`, `addTask`, `addInvoice`, `launchForm`. Legacy HISO left `addInvoice`/`launchForm` as unimplemented no-op stubs (silently reporting `processed=false`); per stakeholder decision (2026-07-19, Section 8 Decision 4), these are built out as real, working actions rather than carried forward as silent no-ops. | HISO `processAction` | FR-TASK-01 |
| `/acc45/sessions/{sessionKey}/delivery-options` | GET | EDI delivery/submission options | HISO `getDeliveryOptions` | — |
| `/acc45/version` | GET | Version/dictionary metadata | HISO `getVersion` | — |

### 4.10 Encounter Summary Templates (KARO-unique)

**Revised 2026-07-19 (Section 8, Decision 5):** `GetEncounterSummary` is implemented for real (a genuine read of the patient's saved encounter-summary data), not carried forward as the legacy mock-data stub and not excluded.

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/encounter-summary-templates/{identifier}/schema` | GET | Schema-driven field/caption/type lookup for a template (diabetes review, foot exam, retinopathy) | KARO `GetTemplateSchema` (internal, exposed as first-class endpoint) | KARO-BR-10 |
| `/patients/{patientId}/encounters/{encounterId}/encounter-summary` | GET | Retrieve a patient's saved encounter summary for a given template `identifier`. Legacy `GetEncounterSummary` returned hardcoded mock data unrelated to the requested patient (KARO-BR-09); rebuilt as a real, working read against the patient's actual saved data. | KARO `GetEncounterSummary` (rebuilt) | KARO-BR-10 |
| `/patients/{patientId}/encounters/{encounterId}/encounter-summary` | POST | Save a templated encounter summary; typed field validation against the schema (replacing loose `JObject` parsing, KARO-BR-11) | KARO `SaveSummary` | §12.13 |

### 4.11 Tasks

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/patients/{patientId}/tasks` | POST | Create a task/reminder from a clinical form action; subject = resolved SNOMED/Read-code concept name + free text (HISO-BR-19/20) | HISO `Task.processTask`/`AddTask` | FR-TASK-01 |

### 4.12 Recalls (KARO-unique)

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/recall-categories` | GET | List recall categories, `group` filter | KARO `GetRecallCategories` | — |
| `/patients/{patientId}/recalls` | GET | List recalls for a patient | KARO `GetRecalls` | — |
| `/patients/{patientId}/recalls` | POST | Save a recall; empty `categoryId` defaults per group (KARO-BR-22, stored-procedure level) | KARO `SaveRecall` | — |

### 4.13 Screening

**Revised 2026-07-19 (Section 8, Decision 5):** `SaveScreeningCode` is implemented for real — with a working authentication check and working persistence — not carried forward with its legacy no-auth/fake-success behavior and not excluded. Reproducing the legacy behavior literally would violate FR-AUTH-04 ("no endpoint may bypass authentication").

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/screening-codes` | GET | List screening codes | KARO `GetScreeningCodes` | — |
| `/patients/{patientId}/encounters/{encounterId}/screening-codes` | POST | Save a screening code result, with real authentication and real persistence. Legacy `SaveScreeningCode` performed no token validation and always reported fake success without persisting (KARO-BR-06) — rebuilt as a working endpoint, not reproduced. | KARO `SaveScreeningCode` (rebuilt) | FR-AUTH-04 |

### 4.14 Providers / Practitioners

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/providers` | GET | List/lookup providers; `practiceLocationId` filter | KARO `GetProvider`; ERMS `GetRegisteredPractitioners` (ERMS-BR-17); COL `GetProviderData` | — |

### 4.15 Practice / Session Context (COL/Pegasus-specific)

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/practices/{practiceId}/context` | GET | Practice/surgery/session context data used by the COL/Pegasus claiming flow | COL `GetSessionData`, `GetSurgeryData` | — |

### 4.16 Billing / Invoicing

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/patients/{patientId}/invoices` | POST | Save an invoice/service record; duplicate submission is a documented non-error (see Section 12); requires elevated `billing:write` scope, distinct from clinical read scope | KARO `SaveInvoice` (KARO-BR-13); COL `SaveInvoice` (ERMS-BR-15) | FR-BILL-01, FR-IDEM-01 |

### 4.17 Tenant / Practice Administration (internal, platform-admin scope only)

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/admin/practices` | POST | Register a new practice in the tenant registry (routing target, no redeploy) | None — net-new, replacing HISO's fixed 4-connection model and KARO/ERMS's `Web.config` convention | FR-TEN-01, FR-ADMIN-01, ADR-001 |
| `/admin/practices/{practiceId}` | GET | Retrieve a practice's registry entry | — | FR-TEN-01 |
| `/admin/practices/{practiceId}` | PATCH | Update a practice's routing/config entry | — | FR-TEN-01, ADR-008 (config toggles) |

### 4.18 Health / Diagnostics

| Endpoint | Method | Purpose | Legacy Source(s) | Requirement |
|---|---|---|---|---|
| `/health` | GET | Liveness/readiness check | KARO `Ping`; ERMS `Ping` | — |

---

## 5. Mode Determination

**Mode: Migration/consolidation.**

Evidence: the SRS is explicitly derived from reverse-engineering three existing production systems (HISO, KARO, ERMS) with a companion cross-system `ComparisonReport.md` and `MigrationRecommendations.md` (Keep/Refactor/Merge/Replace/Remove disposition per module). The EAD and ADR log contain multiple explicit "no consumer-side change" constraints (ADR-002, ADR-004, ADR-007) that only make sense when real, existing external consumers (HSS Portal, ERMS eReferrals platform, COL/Pegasus, HealthLink-style form engines) must continue operating against this contract. Section 8 (Reconciliation Decisions) is therefore required and populated below.

---

## 6. Request/Response Contracts

### 6.1 Canonical Response Envelope

- **Single-resource responses** (GET by ID, POST/PUT results): the resource object is returned directly in the response body — no wrapper. This matches the field-level shape already present in the legacy systems' successful responses (inferred: minimizes translation work in the edge adapters, which is the whole point of the adapter pattern in Reconciliation Decision #1).
- **List responses**: `{ "items": [ ... ] }`. The `items` wrapper key is reserved now so that pagination metadata (`nextCursor`, `total`, etc.) can be added alongside it later as a non-breaking change if the "no pagination in v1" decision (Section 11) is revisited.
- **Timestamps**: ISO 8601 UTC (`2026-07-19T14:30:00Z`). None of the three legacy systems document a canonical timestamp format; this is inferred as the safe modern default and flagged as an inference, not a confirmed legacy behavior.
- **IDs in the canonical contract are plain (decrypted/decoded) values** — `patientId`, `encounterId`, `practiceId` (implied by the authenticated token's scope, not repeated in the path — see Section 13). The legacy Rijndael/AES obfuscation and Base64 layering (KARO-BR-02, ERMS-BR-02/03) is edge-adapter-only translation for legacy consumers; per SRS §12.7, ID obfuscation must not be relied upon as an access-control boundary in the canonical contract.

### 6.2 Representative Contracts

**`GET /v1/patients/{patientId}/demographics`** (synced 2026-07-23 to match `CanonicalDemographicsController` — see Section 4.2)

One endpoint for every consumer. The response shape is the same `DemographicsCanonical` field set for all callers; which fields are actually populated (non-allowed fields are omitted, not just nulled) depends on the caller's `originScope`. Supports `?fields=` to request a subset of the caller's allowed fields (FR-4) — requested fields outside the caller's allowed set are silently dropped, never a 400/403 (FR-5, verified live 2026-07-22, see `PROJECT_STATUS.md`).

Example — KARO-scoped token, `GET /v1/patients/123456/demographics`, response `200`:
```json
{
  "patientId": 123456,
  "practiceId": "302_F3H045",
  "firstName": "string",
  "lastName": "string",
  "dateOfBirth": "1980-01-01",
  "dateOfEnrolment": "2015-03-01",
  "endEnrolmentDate": null
}
```

Example — ERMS-scoped token, same endpoint, response `200` (ERMS's allowed fields only — `practiceId`/enrolment fields never appear, even though they exist on the shared canonical shape):
```json
{
  "patientId": 123456,
  "encounterId": 789,
  "firstName": "string",
  "lastName": "string",
  "dateOfBirth": "1980-01-01",
  "nhi": "string"
}
```

Example — same ERMS-scoped token, `GET /v1/patients/123456/demographics?fields=firstName,practiceId,dateOfEnrolment` (requesting HISO's/KARO's fields on purpose), response `200` — cross-origin fields silently dropped, no error:
```json
{
  "firstName": "string"
}
```

<details>
<summary>Historical record — v1.1's four-separate-endpoint examples (2026-07-19, superseded 2026-07-23)</summary>

Each of the four demographics endpoints returned its own legacy system's field shape as-is (JSON-translated where the legacy shape was XML), rather than one merged/union shape.

```json
{
  "patientId": 123456,
  "practiceId": "302_F3H045",
  "firstName": "string",
  "lastName": "string",
  "dateOfBirth": "1980-01-01",
  "dateOfEnrolment": "2015-03-01",
  "endEnrolmentDate": null
}
```

</details>

**`POST /patients/{patientId}/encounters/{encounterId}/conditions`**

Request body:
```json
{
  "diagnosisCode": "string",
  "description": "string",
  "isLongTerm": true,
  "sideCode": "string",
  "sideDescription": "string"
}
```
`isLongTerm` is a real boolean in the canonical contract — legacy KARO's free-text `"true"`/`"false"` string comparison (KARO-BR-21) is edge-adapter-only. `sideCode`/`sideDescription` preserve HISO-BR-14's split of non-numeric `"code,description"` ACC45 diagnosis "side" values, generalized to the canonical conditions contract since KARO/ERMS's `GetClassifications`/`GetDiagnosisData` do not document an equivalent field — **flagged as inferred generalization**, to confirm with stakeholder whether non-ACC45 conditions actually carry a "side."

Response `201` (new) or `200` (idempotent duplicate — see Section 12):
```json
{
  "conditionId": 789,
  "status": "created",
  "diagnosisCode": "string"
}
```

**`POST /patients/{patientId}/invoices`**

Request body:
```json
{
  "serviceCode": "string",
  "serviceName": "string",
  "amountInclGst": 120.50,
  "payee": "string",
  "serviceProvider": "string",
  "serviceDate": "2026-07-19",
  "pegasusReference": "string",
  "claimShortCode": "string"
}
```
Field set from ERMS COL `SaveInvoice` (`ServiceCode`, `ServiceName`, `AmountInclGST`, `Payee`, `ServiceProvider`, `ServiceDate`, `PegasusReference`, `ClaimShortCode`), since it is the only one of the two legacy invoice endpoints with a documented field list (KARO's `SaveInvoice` request model is not enumerated in `karo/EndpointInventory.md`). **Flagged as inferred**: confirm KARO's `Invoice` model has an equivalent or compatible field set before finalizing — see Section 18, Open Question 4. The legacy random-6-digit-pre-confirmation-code artifact (ERMS-BR-16) is explicitly **not** carried forward (confirmed legacy artifact, not a rule to preserve).

Response `201` or `200` (idempotent duplicate):
```json
{
  "invoiceId": 4501,
  "status": "created"
}
```

### 6.3 Full Field-Level Contracts

Full per-endpoint request/response field tables for every endpoint in Section 4 are deferred to the OpenAPI specification (Section 15), which is the authoritative machine-readable version of every schema referenced here. Any endpoint's field list not explicitly reconciled above uses the response shape of whichever legacy source has the most complete documented field list in its `EndpointInventory.md`/`BusinessRules.md`, flagged inferred where more than one legacy source contributes.

---

## 7. Data Contracts / Shared Schemas

Reused across multiple endpoints, defined once here and referenced by name in the OpenAPI spec:

**`Patient` (summary)** — `{ patientId, practiceId, firstName, lastName, dateOfBirth }`. Referenced by demographics, notes, conditions, medications, lab/radiology, documents, observations, invoices.

**`Practice`** — `{ practiceId, name, phoCode, databaseServerId }`. `databaseServerId` resolves via the tenant registry (ADR-001) and is never exposed to non-admin callers — internal routing detail only.

**`Document`** — `{ documentId, patientId, direction: "in"|"out", contentType, dmsKey, createdAt, subject, referenceId }`. `direction` and `contentType` are both first-class fields, not aliases of each other — see Section 8, Reconciliation Decision #2.

**`Money`** — `{ amount: number, currency: "NZD" }`. Currency fixed to NZD; no legacy system documents multi-currency support and none is implied by any requirement.

**`Error`** — see Section 10 (canonical error shape), referenced by every endpoint's non-2xx responses.

**`ResourceScope`** (not a wire schema — a token-claim shape, documented here for traceability) — `{ patientId, encounterId, practiceId, originScope: "HISO"|"KARO"|"ERMS"|"COL" }`, per ADR-003. Never appears in a request body; always derived from the authenticated token.

---

## 8. Reconciliation Decisions

### Decision 1 — Response format: one canonical JSON contract, edge compatibility adapters preserve every legacy wire shape

**Reconciled:** The canonical Unified Healthcare API contract (Sections 4, 6, 7 above) is JSON, REST-styled, one shape per capability — replacing the three-way split between KARO JSON, ERMS `APIController` XML, and ERMS `COLController` JSON described in SRS §13.3. External consumers are **not required to change**: HSS Portal keeps receiving KARO's existing JSON shapes, the ERMS eReferrals platform keeps receiving ERMS `APIController`'s existing XML shapes, and the COL/Pegasus consumer keeps receiving ERMS `COLController`'s existing JSON shapes — all via edge adapters that translate to/from the canonical contract.

**Evidence justifying this resolution:** SRS §13.3 itself states the analysis reports recommend "one canonical data-access surface with contract adapters if needed" as the resolution path. This is directly consistent with the "zero consumer-side change" constraint the stakeholder already locked in for authentication (ADR-002: "existing consumers... must not be required to change anything on their end"), for HISO's session mechanism (ADR-004), and for HISO's per-server routing (ADR-007) — the architecture's established pattern for every other subsystem is "preserve the exact existing wire contract for existing consumers, put the real change behind an adapter." Applying a different pattern to response format specifically, with no stated reason to, would be inconsistent with every other architecture decision made on this project.

### Decision 2 — Document classification: both axes preserved as first-class fields, not merged

**Reconciled:** HISO classifies documents by declared MIME/content type; KARO/ERMS classify by direction (`in`/`out`). The canonical `Document` schema (Section 7) carries both `direction` and `contentType` as independent, first-class fields rather than collapsing one into the other.

**Evidence:** SRS FR-DOC-03 explicitly requires this reconciliation be "stakeholder-confirmed, not silently inferred," since these are non-equivalent classification axes (SRS §9.4). This contract records the *shape* of the reconciliation (both fields present, independently settable) but the specific filing/storage-category mapping logic that consumes both fields together is an implementation detail for Implementation Planning, not a contract concern — flagged in Section 18 rather than resolved here, since no source document specifies the combined mapping rule.

### Decision 3 — Invoice idempotency and condition-save idempotency: one shared contract, not two undocumented magic-code conventions

**Reconciled:** Both `POST /patients/{patientId}/invoices` (replacing KARO's `-3` and ERMS COL's `-3` magic codes) and `POST /patients/{patientId}/encounters/{encounterId}/conditions` (replacing KARO's `-5` magic code) use the same documented idempotency contract — see Section 12.

**Evidence:** FR-IDEM-01 explicitly calls for "one implementation with a documented idempotency contract" replacing all magic-code conventions; ComparisonReport §4 flags KARO's and ERMS's `-3` codes as *plausibly* the same underlying stored-procedure contract but not confirmed identical — this contract intentionally does not depend on that confirmation, since the canonical contract's idempotency behavior is defined at the API layer, not by passing through whatever the legacy stored procedure returns.

### Decision 4 — HISO's dead-code paths (`static mode`, `addInvoice`, `launchForm`): implemented, not excluded

**Reconciled:** Reversing this document's original v1.0 position (which excluded these as confirmed dead stubs pending sign-off), all three are implemented as real, working capabilities in the unified platform — see Section 4.9.

**Evidence:** direct stakeholder instruction (2026-07-19): "implement it as it is, do not skip anything implemented in old" — read together with the platform-wide principle recorded the same day ("do not remove anything from the new API that any old API already implemented"). This is a deliberate stakeholder scope decision, not an inference from the analysis reports.

### Decision 5 — KARO's `GetEncounterSummary`/`SaveScreeningCode`: implemented for real, not reproduced broken

**Reconciled:** Both endpoints are built as real, working capabilities (Sections 4.10, 4.13) rather than either excluded (this document's original v1.0 position) or reproduced with their confirmed-broken/insecure legacy behavior intact.

**Evidence:** the stakeholder's initial instruction was to "implement as it is... do not skip anything" for both endpoints. Because the literal legacy behavior (`SaveScreeningCode`'s missing auth check; `GetEncounterSummary`'s hardcoded mock data) directly conflicts with FR-AUTH-04 and the platform's own security objectives, this was flagged back to the stakeholder rather than applied literally. The stakeholder then confirmed (2026-07-19) building real functionality while keeping the existing endpoint names/paths/shapes for continuity — the resolution recommended in response to that flagged conflict, adopted as stated.

### Decision 6 — Demographics (and other union-shaped responses): kept separate per legacy system, not merged

**Reconciled:** This document's original v1.0 position (Section 6.1's inferred field union under one merged endpoint) is replaced: HISO, KARO, ERMS, and COL each keep their own demographics endpoint and field shape (Section 4.2), gated by the caller's `originScope` token claim (ADR-003) rather than merged into one canonical shape.

**Evidence:** direct stakeholder decision (2026-07-19) — no sample live responses or time available to do the field-by-field reconciliation Section 6.2 originally assumed; deferred, with "as previously implemented" specified as the interim shape (fully separate endpoints, not a shared path with origin-based switching). This changes Section 18's original Open Question 3 from "resolve this reconciliation" to "revisit merging later if it ever becomes worthwhile," a lower-priority, non-blocking item rather than a contract gap.

**Superseded 2026-07-23:** the "revisit merging later" trigger from this decision was reached — `CanonicalDemographicsController` implements one merged endpoint (Section 4.2) without requiring the field-by-field reconciliation this decision deferred, because per-origin field scoping is enforced independently of whether the underlying shape is merged. This document is now synced to that real implementation; Decision 6's reasoning is left in place above as the historical record of why the four-endpoint design existed for a period, not as the current contract.

---

## 9. Versioning Strategy

**Stakeholder decision (2026-07-19):** versioning must be visible/trackable in documentation, but must introduce **no change that interrupts existing functionality** for any consumer.

**Resolution (original, 2026-07-19):** No version identifier appears in the URL path, in a custom header, or in the media type for v1. The canonical contract launches unversioned in the sense that matters to callers — nothing in a request has to change for the API to evolve within backward-compatible bounds. Versioning is tracked **only** in documentation artifacts: this document's own Document Control table (Section 1), the OpenAPI spec's `info.version` field (Section 15), and an append-only changelog maintained alongside both.

**Superseded 2026-07-23 — two-tier model, matching real code:** the 2026-07-19 decision above was written before the canonical hub controllers existed, for a contract that at the time only covered legacy-compat endpoints. Real code now has two distinct surfaces with two distinct, both-deliberate versioning approaches, confirmed by checking every controller rather than assumed:

- **Legacy-compat endpoints** (`AuthController`'s legacy translators, `HisoCompatController`, `KaroCompatController`, `ErmsCompatController`, `ColCompatController`) — **stay unversioned**, exactly per the original 2026-07-19 resolution above. These must remain byte-identical to what each external consumer already calls (ADR-002/004/007's "zero consumer-side change" mandate), so no URL/header/media-type version identifier is appropriate here. Nothing changes for this surface.
- **Canonical hub surface** (all 15 `Canonical*Controller` classes under `src/Api/Features/Canonical/Controllers/` — `CanonicalDemographicsController`, `CanonicalConditionsController`, `CanonicalDocumentsController`, etc., confirmed via direct grep: 15/15 consistent) — **uses a `/v1/...` URL path prefix**, deliberately and uniformly, not a stray inconsistency in one controller. This has no legacy consumer to preserve compatibility for (it's the new hub surface spec `HEK_UNIFIED_API_SPEC.md` calls for), so URL-path versioning was chosen instead — the standard approach used by both Azure API Management and AWS API Gateway, which is exactly what spec NFR-9 ("research-grounded design... following Azure/AWS API gateway patterns") asks for. A future breaking change to the hub ships as `/v2/...` alongside `/v1/...`, giving a real structural migration path rather than relying purely on documentation discipline.

This document's "Breaking vs. non-breaking" definitions below still apply to both surfaces; the difference is only in *where* a breaking change is expressed (a coordinated migration doc for legacy-compat, a new `/v2` path for the hub).

**Breaking vs. non-breaking, for this contract:**
- Non-breaking (does not require a new documented version, ships anytime): adding an optional request field, adding a new response field, adding a new endpoint, adding a new enum value that callers are expected to ignore if unrecognized.
- Breaking (requires a new major documented version and a explicit, coordinated consumer migration — never silently shipped): removing/renaming a field, changing a field's type or meaning, changing a status code's meaning for an existing scenario, tightening a previously-optional field to required.

**Deprecation policy:** Because existing consumers (HSS Portal, ERMS eReferrals, COL/Pegasus, HealthLink engines) reach the platform through edge compatibility adapters (Section 8, Decision 1), a breaking change to the *canonical* internal contract does not necessarily require any external consumer to change at all — only the adapter needs updating. A breaking change to an *adapter's* external-facing shape is the one category that genuinely affects a legacy consumer, and per the stakeholder's instruction, no such change may ship without explicit, coordinated migration — consistent with ADR-008's broader "nothing new activates without an explicit, tested rollout" philosophy.

---

## 10. Error Handling & Status Code Semantics

**Stakeholder decision (2026-07-19):** error responses must be secure — they must not display data. This directly reinforces SRS §12.14 (no exception/fault detail reaching clients) and §12.8 (security-event logging as the destination for the detail that *is* captured).

**Canonical error shape** (RFC 7807-inspired, data-minimized):
```json
{
  "type": "https://api.hek.example/errors/validation-failed",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields failed validation.",
  "traceId": "00-4bf9...-01",
  "errors": [
    { "field": "diagnosisCode", "message": "This field is required." }
  ]
}
```
`detail` and any per-field `errors[].message` are always generic, static, non-PHI, non-stack-trace strings drawn from a fixed catalog — never interpolated with user input, database error text, or patient data. `traceId` correlates the response to the platform's structured server-side logs (SRS §15), where the full detail an engineer needs is captured under the existing audit/security-event logging requirement (§12.8) — not exposed to the caller. This directly replaces HISO's `includeExceptionDetailInFaults="true"` and KARO/ERMS's `debug="true"` exception leakage (§12.14).

**Status code mapping** (FR-HTTP-01 — replacing KARO/ERMS's confirmed "always HTTP 200" pattern):

| Scenario | Status |
|---|---|
| Successful read | 200 |
| Successful create | 201 |
| Successful update/save with no new resource | 200 |
| Idempotent duplicate write (see Section 12) | 200 (existing resource returned, not an error) |
| Malformed/invalid request body or parameters | 400 |
| Missing/invalid/expired credential | 401 |
| Valid credential, insufficient scope (e.g. clinical-scoped token calling a billing:write endpoint) | 403 |
| Resource not found | 404 |
| Genuine conflict outside the documented idempotency path | 409 |
| Rate limit exceeded (§12.9) | 429 |
| Unhandled server error | 500 |

No scenario returns `200` with an error communicated only in the body — this is a hard requirement, not a style preference (FR-HTTP-01 acceptance criteria).

---

## 11. Pagination, Filtering, Sorting

**Stakeholder decision (2026-07-19):** keep list endpoints unpaginated — third-party consumers (HSS Portal, ERMS eReferrals platform, COL/Pegasus) have no pagination handling on their end today, and none of the three legacy systems paginates (SRS §13.5).

**Pagination:** Not implemented in v1. Every list endpoint in Section 4 returns the full filtered result set, wrapped in `{ "items": [...] }` (Section 6.1). The `items` wrapper is reserved specifically so pagination metadata can be introduced later as a non-breaking addition (new optional fields alongside `items`) if result-set sizes become a real operational problem — this is a scalability risk carried forward explicitly, not silently ignored (see Section 19, Risks).

**Filtering:** Preserved from confirmed legacy patterns — date-range filtering (`sinceDate`/`untilDate` or `minDate`/`maxDate` depending on endpoint) on all list endpoints that legacy systems filtered by date (ERMS `GetConsultNotes`, `GetLaboratoryReportList`, etc., per SRS §13.6), plus endpoint-specific filters carried from the endpoint inventory (e.g. `direction`, `contentType`, `referenceId` on documents; `conceptId` on observations; `practiceLocationId` on providers).

**Sorting:** `sortOrder=asc|desc` on date fields, preserved from ERMS's `pmsOrder` parameter and its human-readable `Order` annotation pattern (ERMS-BR-12, SRS §13.7) — generalized to every list endpoint in the canonical contract for consistency, since no legacy system contradicts this pattern (KARO/HISO simply didn't have an equivalent parameter, not a conflicting one).

---

## 12. Idempotency Contract

Replaces KARO's `-5` (diagnosis already exists) and both KARO's and ERMS COL's `-3` (invoice already exists) magic return codes (FR-IDEM-01, KARO-BR-12/13, ERMS-BR-15).

**Contract:** `POST` endpoints subject to duplicate-submission risk (`conditions`, `invoices`, `documents`) accept an optional `Idempotency-Key` request header. If a request with a previously-seen key (scoped to the same patient+encounter+practice) is resubmitted:
- If the prior request succeeded, the endpoint returns **`200 OK`** with the original created resource (not a new one, not an error) — preserving the legacy *business intent* (duplicate submission is a non-error success, KARO-BR-12/13, ERMS-BR-15) through a documented mechanism instead of a numeric code.
- If no `Idempotency-Key` is supplied, the endpoint falls back to natural-key duplicate detection where one exists (e.g. same diagnosis code for the same appointment) and applies the same "existing resource, `200`, not an error" behavior — this preserves legacy behavior for the two edge adapters (KARO, ERMS COL) that will not supply the new header, since neither legacy consumer knows about it.
- A genuine conflict that is *not* a recognized duplicate (e.g. concurrent conflicting edits) returns `409 Conflict` with the canonical error shape (Section 10).

This satisfies FR-IDEM-01's acceptance criteria: no magic numeric codes remain in the public contract, and the behavior is documented and testable.

---

## 13. Authentication & Authorization Contract

Sourced from ADR-002 and ADR-003 — not re-decided here.

**Token issuance (ADR-002):** `POST /auth/token` is the canonical entry point. Edge adapters preserve each legacy consumer's exact existing request shape (HSS Portal's username/password/patientId/encounterId/system/pho; ERMS's XML `Credential`; COL's JSON `Credential`) and translate to this call. Internally, each legacy service account (`hsslive`, ERMS/COL equivalents) is registered as a real service-account credential in **Microsoft Entra ID** (vendor confirmed per the ADR follow-up decision log). On successful validation, the platform issues a signed, short-lived internal token.

**Token claims (ADR-003):** every issued token is scoped to exactly one `patientId` + `encounterId` + `practiceId`, plus a structurally-determined `originScope` claim (`HISO`, `KARO`, `ERMS`, or `COL`) set by which credential/entry-point authenticated the request — **never** by a caller-supplied field. `COL`'s origin scope is distinct from `ERMS`'s other functions (confirmed in the ADR follow-up log, given COL's financial `SaveInvoice` write).

**Transport:** `Authorization: Bearer <token>` header on every protected endpoint. `/auth/token` and `/health` are the only unauthenticated endpoints.

**Expiry (ADR-002/ADR-004):** 12-hour expiry, uniform across all origin scopes (matching ERMS's existing window, now also applied to HISO's session and to KARO's previously-unenforced `expiryInDays`). No early-revocation/kill-switch mechanism exists (confirmed decision, ADR follow-up log) — expired-only, no revocation-before-expiry.

**HISO exception:** HISO's existing `SessionGUID` mechanism is preserved as-is (ADR-004/ADR-007) — the HealthLink-style form engine never calls `/auth/token`. The SOAP edge adapter resolves the `SessionGUID` via the same unchanged Indici lookup and mints an internal resource-scoped token with `originScope: "HISO"` for use against the canonical contract (Section 4.9).

**Authorization enforcement:** every protected endpoint validates the token's `patientId`/`encounterId`/`practiceId` claims against the resource(s) being accessed (FR-AUTH-02) and its `originScope` against which capability is being called (ADR-003) — a `KARO`-origin token cannot reach `ACC45`-only or `COL`-only endpoints, and vice versa. Financial write endpoints (`/patients/{patientId}/invoices`) additionally require a `billing:write` scope, distinct from the read-only clinical scope every other endpoint accepts (SRS §12.2, ERMS SEC-04).

---

## 14. Rate Limiting Contract

Net-new — confirmed absent in all three legacy systems (SRS §12.9). Communicated via standard headers: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`. Exceeding the limit returns `429 Too Many Requests` with the canonical error shape and a `Retry-After` header.

**Rollout (stakeholder decision, 2026-07-19):** rate limiting is built as a real, working capability — not omitted, and not deferred indefinitely — but rolled out via the same config-toggle pattern already established for authentication and origin-scoping (ADR-008): generous or effectively-off limits at launch, so existing consumer traffic patterns aren't disrupted on day one, tightened once monitoring confirms real thresholds are safe. This mirrors the project's existing "run in legacy-equivalent mode first, enable new behavior incrementally" rollout philosophy rather than either shipping with the legacy systems' literal zero protection or an untested strict limit from day one.

Specific limit thresholds are still an Implementation Planning / Infrastructure Design concern (not confirmed by any source document) — flagged in Section 18.

---

## 15. OpenAPI Specification

See companion file `Unified-Healthcare-API_openapi.yaml`, alongside this document. It covers every endpoint in Section 4 with schemas from Sections 6–7, and must be updated together with this document — it is the machine-readable version of Sections 4, 6, 7, 9, 10, 12, and 13.

---

## 16. Backward Compatibility / Consumer Migration Plan

| Existing Consumer | What Changes | What Doesn't | Cutover Validation |
|---|---|---|---|
| HSS Portal (KARO consumer) | Nothing at the wire level. Internally: `hsslive` service account re-registered in Entra ID (ADR-002); requests now pass through the auth/origin-scope layer instead of `uspInsertAndValidateToken`. | Exact `Authenticate` request/response shape, all `GetXxx`/`SaveXxx` JSON shapes, endpoint paths/casing. | Run the merged API in "legacy-equivalent" mode (ADR-008, security toggled off) against HSS Portal traffic first, validate identical responses, then enable auth/origin-scoping incrementally, one capability at a time, per the stakeholder's explicit staged-rollout preference. |
| ERMS eReferrals platform | Nothing at the wire level for `APIController`'s 23 XML endpoints. Internally: ERMS's service account re-registered in Entra ID; `originScope: "ERMS"`. | Exact XML request/response shapes, endpoint paths. | Same staged validation approach as HSS Portal. |
| COL/Pegasus claiming system | Nothing at the wire level for `COLController`'s 7 JSON endpoints — **confirmed live in production** (stakeholder, 2026-07-19). Internally: distinct `originScope: "COL"`, separate from ERMS's other functions per the ADR follow-up decision. | Exact JSON request/response shapes. | Same staged validation approach as HSS Portal/ERMS. |
| "Azure ERMS API" mirror (`ERMSAPIProxy.cs`) | **Confirmed not currently live** (stakeholder, 2026-07-19), but the forwarding capability is **retained in the platform, not removed** — kept present and dormant-safe rather than deleted, consistent with the "don't remove anything old APIs implemented" principle. | The forwarding mechanism itself, should it ever need reactivating. | No cutover validation needed while dormant; revisit if the stakeholder confirms it needs to go live again. |
| HealthLink-style form engine (HISO consumer) | Transport scheme only: `http://` → `https://` (ADR-009) — the one explicitly-approved exception to "no consumer change." Nothing else. | SOAP contract, `SessionGUID` mechanism, per-server addressing (ADR-007). | Verify each existing HISO server address responds identically over HTTPS before decommissioning the HTTP listener. |

---

## 17. Contract Review

| Category | Rating | Rationale | Recommendation (if not PASS) |
|---|---|---|---|
| Consistency | PASS | Every canonical endpoint follows the same `/patients/{patientId}/...` resource shape, the same envelope (Section 6.1), the same error shape (Section 10), and the same idempotency mechanism (Section 12) across all domains. | — |
| Versioning Safety | WARNING (legacy-compat surface only) | Updated 2026-07-23: Section 9 is now a two-tier model. The canonical `/v1` hub surface has a real structural safety net — a future breaking change ships as `/v2`, not a silent in-place edit. The original WARNING still applies to the legacy-compat surface only, which stays unversioned by design (must remain byte-identical to existing consumers) and so still relies on process discipline (no breaking change without a coordinated migration) rather than a URL-level mechanism. | Add a lightweight breaking-change review gate to Implementation Planning for the legacy-compat surface specifically (e.g. a required sign-off checklist referencing Section 9's breaking/non-breaking definitions). Not needed for the hub surface, which already has `/v2` as its safety net. |
| Backward Compatibility (migration mode) | PASS | Every existing consumer's exact wire shape is preserved via edge adapters (Section 8, Decision 1; Section 16); the only approved consumer-visible change in the entire contract is HISO's HTTP→HTTPS transport switch (ADR-009), already stakeholder-approved. | — |
| Discoverability | WARNING | A new consumer (e.g. a future practice-management integration) can find every canonical endpoint from this document and the OpenAPI spec, but several endpoints (ACC45 session flow, COL/Pegasus context) still carry legacy-shaped concepts (`sessionKey`, `formInstanceId`) that aren't self-explanatory without reading the source legacy system's business rules. | Add a short domain-concept glossary to the OpenAPI spec's description fields, not just this document's Appendix, so the machine-readable contract is self-contained too. |
| Extensibility | PASS | The `items` list envelope reserves room for pagination metadata; the idempotency contract and error shape are additive-safe; new capabilities can be added as new resources without touching existing ones, consistent with ADR-008's incremental-rollout philosophy. | — |
| Security Exposure | WARNING | The contract itself is sound (data-minimized errors, resource-scoped tokens, structural origin-scoping), but it inherits a real platform-level risk from ADR-008: authentication itself is a config toggle with no automated production block, and this contract cannot enforce that toggle's state — a caller could, in principle, be talking to a canonically-correct contract with authentication silently switched off. | This is called out explicitly rather than downplayed: production readiness (Section 20) must not be declared until ADR-008's open item (an owner and enable-by date for the security toggle) is resolved, independent of this contract being otherwise complete. |
| Documentation Completeness | WARNING | Every endpoint has a purpose, legacy traceability, and requirement reference (Section 4); most have field-level examples (Section 6.2), but not every endpoint in Section 4 has a worked request/response example in this document — the OpenAPI spec is relied upon to fill that gap completely. | Confirm during OpenAPI authoring (Section 15) that every endpoint has at least one full example; do not let this document's partial coverage stand in for the spec's completeness. |

**Overall: not a clean PASS.** Three WARNINGs are recorded honestly above, none is a FAIL, and none blocks handoff to Implementation Planning — but Security Exposure's WARNING is inherited from an already-accepted platform-level risk (ADR-008) that this document did not create and cannot resolve on its own; it is flagged here so it isn't lost between architecture and implementation.

---

## 18. Open Questions

Revised 2026-07-19: several original open questions were resolved directly by the stakeholder in a follow-up Q&A round and are now recorded as Reconciliation Decisions (Section 8, Decisions 4–6) rather than repeated here. What remains:

1. The combined MIME-type + direction filing/storage-category mapping logic for documents (Section 8, Decision 2) — the contract fixes the *shape* (both fields present) but not the mapping rule; no source document specifies it.
2. Specific rate-limit thresholds (Section 14) — no source document provides numbers; an Infrastructure Design / Implementation Planning input. The rollout mechanism (config-toggle) is decided; the numeric thresholds are not.
3. KARO's `SaveInvoice` request field list is not documented in `karo/EndpointInventory.md`; Section 6.2's invoice contract is inferred from ERMS COL's `SaveInvoice` model only. Needs a source/live-system check to discover KARO's actual fields — whatever is found carries forward as-is, nothing dropped, per the stakeholder's platform-wide "don't remove anything old APIs implemented" principle.
4. Full field-level reconciliation of the four separate demographics endpoints (Section 4.2) into one canonical shape — explicitly deferred by stakeholder decision (Section 8, Decision 6). Not currently blocking; revisit only if/when sample live responses become available.
5. Whether ERMS has an equivalent hardcoded PHO override for practice `302_F3H045` (KARO-BR-04) — stakeholder is verifying this directly; if it turns out to be more than a `Web.config`-only difference, the `/admin/practices` registry entry shape (Section 4.17) may need an explicit override field.

**Resolved this round (see Section 8, Decisions 4–6 for full detail):** HISO's dead-code disposition (implement, don't skip); `GetEncounterSummary`/`SaveScreeningCode` disposition (implement for real, don't reproduce broken behavior); COL/Pegasus liveness (confirmed live); Azure ERMS mirror liveness (confirmed not live, capability retained); cloud host (own servers now, cloud-agnostic for future portability); Aspose (keep, as previously implemented); ADR-008 security-toggle ownership (Zohaib, self-managed timing).

---

## 19. Risks

- **Unbounded list responses at target scale.** Per the stakeholder's explicit "no pagination in v1" decision (Section 11), every list endpoint returns a full result set. At the stated 10,000-concurrent-user target (SRS §5.5), a small number of patients/practices with very large note/document/lab histories could produce disproportionately large responses under concurrent load. Mitigation is intentionally deferred (the `items` envelope is pagination-ready), but this is a real, accepted risk, not an oversight — should be monitored post-launch and revisited if it becomes a measured problem, consistent with the stakeholder's own risk-acceptance pattern elsewhere in this project (e.g. skipping RLS in ADR-001, deferring the security toggle in ADR-008).
- **Idempotency-key adoption gap for legacy edge adapters.** Section 12's `Idempotency-Key` header is a new mechanism; the KARO and ERMS COL edge adapters will not supply it (their legacy consumers don't know it exists), so those paths rely on natural-key duplicate detection instead. If a future capability's "natural key" isn't as clean as diagnosis-code-per-appointment or invoice-per-service, the fallback path may not reliably prevent duplicates for legacy-origin traffic.
- **Field-level union inference (Open Question 3) shipping unverified.** If Implementation Planning proceeds on Section 6.2's inferred demographics field union without the field-by-field reconciliation being done first, a genuine semantic conflict (e.g. two systems using the same field name for different concepts) could ship silently.
- **Contract completeness depends on COL/Pegasus and Azure-mirror status (Open Question 5).** If either turns out to be defunct, Sections 4.15/4.16 and part of Section 13 (COL's distinct origin scope) are unnecessary scope that should be pruned before implementation, not built and later discovered unused. If either turns out to be live and this contract proceeds without confirming it, the opposite risk applies.
- **Security-toggle inheritance (Section 17, Security Exposure WARNING).** This contract's authorization model (Section 13) is only as strong as ADR-008's toggle actually being switched on in production before go-live — a contract-level risk this document cannot close on its own.

---

## 20. Acceptance Criteria

"Ready for Implementation Planning" for this specific contract means:

1. Every endpoint in Section 4 has a corresponding, complete OpenAPI schema (closes the Documentation Completeness WARNING in Section 17).
2. Open Questions 1–2 and 5 (Section 18) — dead-endpoint disposition and COL/Azure-mirror liveness — are resolved with the business, since they determine whether several Section 4 endpoints exist in the build at all.
3. Open Question 3 (demographics/field-union reconciliation) has at least a documented plan (even if not fully executed) for validating the inferred field union against live legacy responses before the canonical demographics endpoint is built.
4. A named owner and enable-by date exists for ADR-008's security toggle (inherited dependency, Section 17), tracked in `PROJECT_STATUS.md`, not newly re-litigated here.
5. The idempotency contract (Section 12) and error shape (Section 10) are implemented as designed in at least one vertical slice (e.g. the conditions endpoint end-to-end) before being assumed correct for every other endpoint that reuses them.

---

## 21. Appendix

### 21.1 Glossary

See `SRS_UnifiedHealthcareAPI.md` §3.3/§3.4 for full domain term/acronym definitions (PMS, DAL, ACC45, HSS, COL/Pegasus, PHO, DMS, encounterId). Contract-specific additions:

| Term | Definition |
|---|---|
| Edge adapter | A translation layer at the API boundary that preserves one legacy consumer's exact existing request/response wire shape while calling the canonical internal contract underneath. |
| Canonical contract | The single REST/JSON API defined in this document — the "one implementation per capability" that all edge adapters translate to/from. |
| Origin scope | A token claim (Section 13) identifying which legacy consumer category (HISO/KARO/ERMS/COL) authenticated a request, determined structurally, never by a caller-supplied field (ADR-003). |
| Resource-scoped token | A token whose validity is limited to one specific patient + encounter + practice combination (ADR-003), not a general-purpose session. |

### 21.2 References

- `docs/SRS_UnifiedHealthcareAPI.md` v1.0
- `docs/architecture/Unified-Healthcare-API_EAD.md`
- `docs/architecture/Unified-Healthcare-API_ADRs.md` (ADR-001–011 + follow-up decision log)
- `docs/analysis/ComparisonReport.md`, `docs/analysis/MigrationRecommendations.md`
- `docs/analysis/hiso/`, `docs/analysis/karo/`, `docs/analysis/erms/` (`EndpointInventory.md`, `BusinessRules.md`)
- `PROJECT_STATUS.md`

---

*End of API Contract Design Document. This document is the primary input to Implementation Planning (Phase 11). No further architecture-level decision should be required to begin building against this contract, subject to the Open Questions in Section 18 being resolved with the business.*
