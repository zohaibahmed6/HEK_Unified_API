# ERMS Web API — Documentation Gap Analysis

## Summary
`ERMS_doc.md` (last revised 05/03/2019, v1.1.2) accurately documents the shape and sample payloads of `APIController`'s 23 XML endpoints for the ERMS eReferrals consumer, but it is entirely silent on `COLController` (7 endpoints, including a financial write), the practice-id-in-EncounterId routing mechanism, the ID-obfuscation/encryption scheme, the Azure-forwarding proxy, and several per-endpoint behavioral defaults found only in code. Per the task's ground rules, the implementation is treated as authoritative everywhere the two disagree.

## Findings (implementation wins; discrepancy noted per row)

### 1. Entire undocumented controller: `COLController`
The doc describes only the ERMS/HISO-concept API. `COLController` (`/col/authenticate`, `/col/getcurrentpatientdata`, `/col/getsessiondata`, `/col/getproviderdata`, `/col/getsurgerydata`, `/col/getdiagnosisdata`, `/col/saveinvoice`) is not mentioned anywhere in `ERMS_doc.md`.
- **Trusting:** code (only source of truth available).
- **Impact:** a reader of the doc alone would not know this API has a second, JSON-based, financially-relevant surface at all. See EndpointInventory.md.

### 2. Practice-ID-in-EncounterId mechanism is undocumented
The doc states (section "Practice ID"): *"The practice Id supplied to invocation URL indicates the user current practice"* and shows a separate query parameter `pmsPracticeId` in the example invocation URI (`&pmsPracticeId=6`). The actual code **never reads a `pmsPracticeId` query parameter in either controller** — instead, every action parses the practice id out of the `EncounterId` string itself via `"_"`/`"__"` delimiter splitting (BusinessRules.md BR-01).
- **Trusting:** code — no controller action in either `APIController.cs` or `COLController.cs` declares a `pmsPracticeId` parameter; grep of both controllers confirms only `pmsPatientId`, `pmsEncounterId`, `pmsOrder`, `pmsMinDateTime`, `pmsMaxDateTime`, `pmsReferenceId`, `pmsUserId`, `LocationId`/`pmsLocationId` are accepted.
- **Impact:** the doc's described invocation contract does not match how the API actually resolves tenant/practice context — a significant discrepancy for anyone building a new client against the documented contract.

### 3. ID obfuscation/encryption/Base64 layering is undocumented
The doc's Authentication section describes the token mechanism but says nothing about Patient ID / Encounter ID being (a) optionally Base64-encoded, then (b) decrypted via a custom Rijndael scheme, with a fallback to treating the value as a plain integer if parsing succeeds. Sample values in the doc (`patientId = 941819`) are plain integers, consistent with the "plain int" fallback path but not representative of the obfuscated path actually used with the `EncounterId`'s embedded practice-id and PHO segments.
- **Trusting:** code (`Models/EncryptionManager.cs`, `GetDcrptValue`/`GetBase64Value` in both controllers).
- **Impact:** doc readers would not know IDs can be (and in practice with multi-segment EncounterIds, must be) obfuscated/structured strings, not bare integers.

### 4. Azure-forwarding proxy is undocumented
`Helpers/ERMSAPIProxy.cs` and the `EnableAzureERMSAPI`/`AzureEMRSAPI` app settings, plus the `"azure"`-substring PHO-segment trigger, have no mention in the doc.
- **Trusting:** code.
- **Impact:** doc gives no indication that some requests may be silently served by a different backend entirely.

### 5. Root element name mismatch: Next of Kin
Doc sample response root element is `<Next_Of_Kin>` (doc line ~337: `<Next_Of_Kin xmlns:xsd=... conceptType="List">`). The code's model class and controller both reference `NextOfKin` as the object/collection name (`NextOfKin objNextOfKin = new NextOfKin(); ... objNextOfKin.PatientNOK.AddRange(...)`).
- **Trusting:** code for the actual XML root name, since `PrepareXml<NextOfKin>(objNextOfKin)` serializes using the `NextOfKin` type's `XmlRoot`/class name conventions.
- **Impact:** low if `NextOfKin`'s `XmlRoot` attribute in `Models/APIModels.cs` is in fact configured to emit `Next_Of_Kin` (not confirmed by grep alone in this pass) — flagged as a discrepancy to verify against a live response if the exact serialized root tag matters to a downstream consumer.
> Assumption: the precise serialized root tag name could not be fully confirmed without inspecting every `[XmlRoot]`/`[XmlType]` attribute in `Models/APIModels.cs` line-by-line; this is called out as a gap to verify, not a confirmed defect.

### 6. Consult Notes default date window undocumented
Doc lists Min/Max DateTime as purely optional filters for `GetConsultNotes` with no mention of a default. Code defaults to a 24-month lookback window when neither is supplied (BusinessRules.md BR-05).
- **Trusting:** code.
- **Impact:** a client omitting date parameters, expecting "all consult notes" per the doc's general pattern for other list endpoints, will actually receive only the last 24 months.

### 7. `SaveDocument`'s generic (non-referral-only) scope is documented, matches code
The doc explicitly notes `SaveDocument` supports any document type, not just referrals — this matches the code's generic `DocumentTypeID`/`SaveToDMS` handling. **No gap** — flagged here as a confirmed match, not a discrepancy.

### 8. Response status codes not documented anywhere
The doc shows only success/fail XML *bodies*; it never states that both success and failure responses are returned with HTTP 200 (SecurityAnalysis.md SEC-05), nor that `SaveDocument` is the one action that can return HTTP 400.
- **Trusting:** code.
- **Impact:** integrators relying on HTTP status codes (reasonable expectation, not stated as wrong in doc) would need to inspect the body regardless.

### 9. AWS vs. on-prem DMS duality is undocumented
Doc's "Save/Upload Document" and "Scanned/Discharge" sections describe a single logical DMS with no mention that storage may be routed to AWS per-practice (BusinessRules.md BR-09).
- **Trusting:** code.
- **Impact:** low for the external consumer (the split is transparent to them), but important for anyone maintaining or migrating the backend.

### 10. Field/date-format corrections mentioned in the doc's changelog but not re-verified against current code
The doc's version history (table at the end of `ERMS_doc.md`) records numerous point-in-time bug fixes (e.g., "Correction: Work and Residence phone swap in Patient Data," "HUC/CSC data swap," "Correction of Level 2 ethnicity code") that are presumed fixed as of v1.1.2 but were not independently re-verified line-by-line against `Models/APIModels.cs` field ordering in this pass.
> Unable to verify from available source within this pass's scope — flagged for a follow-up targeted review of `Models/APIModels.cs` field-by-field against the doc's sample XML if this level of precision is required before building a compatible client.

## Evidence
- `E:\claude_projects\hek_analysis\docs\_source_docs\ERMS_doc.md` (full document, 781 lines)
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Controllers\APIController.cs`, `COLController.cs`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Models\EncryptionManager.cs`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Helpers\ERMSAPIProxy.cs`

## Risks
- Building a new client purely from `ERMS_doc.md` would produce a client that cannot correctly derive practice context (gap #2) and would miss an entire second API surface (gap #1) — both are severe enough to block a documentation-only migration approach.
- The doc's own changelog shows a history of subtle field-swap bugs, suggesting the XML contract has drifted before and should not be assumed stable without direct verification against current code for any field the unified platform intends to consume verbatim.

## Recommendations
- Treat `ERMS_doc.md` as a useful reference for the *shape* of `APIController`'s XML payloads only; do not use it as the source of truth for authentication, routing, or the existence of `COLController`.
- Before building the unified platform's ERMS-equivalent contract, run a live request against a test ERMS instance (if available) to confirm exact XML root/field names in `Models/APIModels.cs`, since this pass could not exhaustively cross-check every field.
- Document `COLController`'s 7 endpoints from scratch as part of this migration effort, since no prior documentation exists for them.
