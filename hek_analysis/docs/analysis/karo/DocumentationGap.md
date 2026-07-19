# KARO — Documentation Gap Analysis

**Summary:** `KARO_HSS_doc.md` (v2.1.3, last updated 2021-05-03) is largely accurate for the endpoints it documents, but it omits at least three implemented endpoints/behaviors entirely, doesn't describe the encrypted-ID/multi-tenant routing mechanism the whole API depends on, and doesn't mention that two operations (`GetEncounterSummary`'s mock responses, `SaveScreeningCode`'s no-op behavior) don't do what a reader would reasonably assume. Per instructions, implementation is treated as authoritative below; every gap states which side is trusted and why.

## Findings

### 1. Undocumented endpoint: `GET /api/GetPatientAttachment`
- **Doc:** No section exists for this operation anywhere in `KARO_HSS_doc.md`.
- **Code:** Fully implemented action returning base64-encoded attachment binary content, with rich filtering (`referenceID`, `sortOrder`, `Subject`, `dateFrom`, `dateTo`) (`Controllers\APIController.cs` lines 1192-1268).
- **Trusting:** Code — it is a real, callable, token-validated endpoint.
- **Gap type:** Missing endpoint documentation.
- **Impact:** Any integrator relying solely on the spec doc would not know this endpoint exists, yet it returns PHI (attachment content) and should be governed by the same data-handling agreements as documented endpoints.

### 2. Undocumented endpoint: `GET /api/GetEncounterSummary`
- **Doc:** Only the **POST** side of "Encounter Summary" (`/api/SaveSummary`) is documented, with three worked examples (DIAP, DS:FS, DS:RET).
- **Code:** A separate GET action exists (`Controllers\APIController.cs` lines 406-450) that, after validating the token, returns **hardcoded, static sample JSON** for `identifier=diap`/`cvra` (with a fixed fake `patientId":"941272"` regardless of the requested patient) and an empty object `{}` for any other identifier — it never queries the database.
- **Trusting:** Code for existence/behavior — but flagging this as almost certainly **not the intended production behavior** (see `BusinessRules.md` BR-09). This is not really "the doc is wrong," it's "the code appears to be an unfinished/mock implementation that was never removed."
- **Gap type:** Missing documentation + likely broken/stub implementation.
- **Impact:** High — if any consumer calls this GET endpoint expecting real data, they receive fabricated sample data silently indistinguishable from a real response.

### 3. Undocumented endpoint: `GET /api/Authenticate`
- **Doc:** Only documents `Authenticate` as a **POST** operation with a JSON body.
- **Code:** A second, GET-based overload exists accepting `username`/`password` as query-string parameters (`Controllers\APIController.cs` lines 30-91).
- **Trusting:** Code for existence — this is a real, callable endpoint. The doc's implicit assumption ("credentials are only ever POSTed") is incomplete.
- **Gap type:** Missing endpoint + undocumented security-relevant behavior (credentials in URL).
- **Impact:** High from a security-review perspective — see `SecurityAnalysis.md`; also a compliance gap since the doc's guidance ("preferably included in the header... or as a parameter") arguably endorses this pattern in prose (`KARO_HSS_doc.md` line 12) but never shows the actual GET query-string endpoint or warns about its risk.

### 4. Undocumented: `SaveScreeningCode`'s actual (non-)behavior
- **Doc:** The changelog (`KARO_HSS_doc.md` line 818) references "POST operations: SaveClinicalNotes, SaveScreeningCodes" as introduced in v2.0.1, but the body of the document has **no dedicated section, request sample, or response sample** for it.
- **Code:** `Controllers\APIController.cs` lines 1924-1947 — the action deserializes and logs the incoming payload, then unconditionally returns `{"status":"success","message":""}` without validating the token or persisting anything.
- **Trusting:** Code for current behavior. Cannot determine from either source what the *intended* contract was, since the doc never fully specified it.
- **Gap type:** Both incomplete documentation and (likely) incomplete/regressed implementation.
- **Impact:** High — a consumer following the changelog reference would reasonably believe screening codes are being saved; none are.

### 5. Undocumented: encrypted ID / multi-tenant routing scheme
- **Doc:** Describes Patient ID and Encounter ID purely as opaque strings passed through unchanged (`KARO_HSS_doc.md` "Patient ID" and "Encounter ID" sections, lines 16-23) — no mention of encryption, no mention of the `__practiceId` suffix convention, no mention that a single deployment serves multiple practices with different DB connection strings.
- **Code:** Every action parses `encounterId` for a `__`-or-`_`-delimited practice suffix and decrypts the leading segment (`Controllers\APIController.cs`, e.g. lines 42-60); `patientId`/`userId` are separately decrypted via `GetDcrptValue()` (line 2027).
- **Trusting:** Code — this is core, load-bearing routing/security logic completely absent from the spec.
- **Gap type:** Missing architectural/business-rule documentation.
- **Impact:** High for anyone trying to reimplement or integrate against this API from the doc alone — the doc's sample `encounterId` values (e.g. `"28999606_491"`) hint at the underscore convention in examples but never explain what the suffix means or that it selects a database connection.

### 6. Undocumented: hardcoded PHO override for practice `302_F3H045`
- **Doc:** No mention anywhere of practice-specific overrides.
- **Code:** Multiple Save* actions force `pho = "SCDHB"` when the practice suffix contains `302_F3H045` (`Controllers\APIController.cs`, e.g. lines 1037-1038, 1651-1652, 1720-1721, 1796-1797, 1883-1884).
- **Trusting:** Code.
- **Gap type:** Undocumented business rule (see `BusinessRules.md` BR-04).
- **Impact:** Medium — a hidden special case that could confuse debugging for that specific tenant if not known.

### 7. Documented but response-code assumption is misleading
- **Doc:** Sample JavaScript (`KARO_HSS_doc.md` lines 52-64) checks `this.status == 200` before parsing the response, implying `200` means success and (by omission) other codes mean failure — standard REST assumption.
- **Code:** `SetToJson()` (`Controllers\APIController.cs` lines 2010-2019) returns **HTTP 200 for every outcome**, including authentication failures and unhandled exceptions; the doc's own "FAIL RESPONSE" examples (e.g. line 104-108) are JSON bodies with `"status":"fail"` returned under a 200 status, which the sample code's `this.status == 200` check would treat as "OK" and proceed to parse — this is technically consistent with the doc's own examples, but the doc never explicitly states "always check the status field, never rely on the HTTP code," which is a meaningful omission for integrators used to REST conventions.
- **Trusting:** Code for actual HTTP status behavior; doc's examples are consistent with it but the guidance is incomplete.
- **Gap type:** Incomplete integration guidance.
- **Impact:** Low-Medium — mostly a clarity gap, not a contradiction.

### 8. Consistent areas (no gap found)
The following documented operations were verified to match their implementation in structure, route name, and general request/response shape: `Ping`, `Authenticate` (POST), `GetDemographics`, `GetProvider`, `GetMedications`, `GetClinicalNotes`/`SaveClinicalNotes`, `GetScreeningCodes`, `SaveSummary` (DIAP/DS:FS/DS:RET templates), `GetLabResults`, `GetDocuments`/`SaveDocument`, `GetObservations`/`SaveObservations`, `GetConditions`/`SaveCondition`, `GetRecalls`/`SaveRecall`, `GetRecallCategories`, `SaveInvoice`. Minor field-level differences (e.g., exact casing, presence of `dateOfEnrolment`/`endEnrolmentDate` in demographics) were reflected in both doc and code (`KARO_HSS_doc.md` line 846 changelog entry vs. `Models\APIModels.cs` `DemographicInfo` class lines 40-41).

## Evidence
Citations inline above; doc source: `E:\claude_projects\hek_analysis\docs\_source_docs\KARO_HSS_doc.md`; code source: `E:\NZTFS\hsswebapi\DevLocal\HSSWebAPI\Controllers\APIController.cs`.

## Risks
- Items 1-4 above represent real functional gaps between what integrators are told exists and what actually runs in production — the two "silently degraded" endpoints (`GetEncounterSummary`, `SaveScreeningCode`) are the highest risk because they fail silently with a plausible-looking success/data response rather than an obvious error.

## Recommendations
- Before the unified platform's design phase, get authoritative sign-off from the client on the true intended behavior of `GetEncounterSummary` and `SaveScreeningCode` — do not infer intent from the doc, since the doc itself is incomplete/silent on both.
- Use `EndpointInventory.md` and `BusinessRules.md` (derived from code) as the source of truth for endpoint behavior in any downstream SRS work, and use this file to flag every place where that departs from `KARO_HSS_doc.md`.
