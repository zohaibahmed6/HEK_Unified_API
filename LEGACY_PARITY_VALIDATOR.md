# Legacy Parity Validator

Tracks operation-by-operation parity between the 4 legacy reference APIs
(HISO, ERMS, KARO/HSS, COL) and the unified API's legacy-compat endpoints.
Statuses: ⬜ Unchecked · ✅ Confirmed match · ❌ Gap/mismatch found

Sources used: `hek_analysis/docs/analysis/{hiso,erms,karo}/EndpointInventory.md`
(ERMS file also contains the COL table). Unified controllers:
`src/Api/Features/Hiso/Controllers/HisoCompatController.cs`,
`src/Api/Features/Auth/Controllers/{ErmsCompatController,KaroCompatController,ColCompatController}.cs`.

## HISO
| Operation | Legacy behavior (source) | Unified endpoint | Status | Notes |
|---|---|---|---|---|
| getVersion | Returns hardcoded application/version/dictionaryVersion/hisoversion (EndpointInventory.md:15) | POST /hiso/getVersion | ✅ | Field-diffed 2026-07-27: `GetVersionResponse.Real()` (Adapters.Hiso) hardcodes application="PMS", applicationVersion="1.0", hisoversion=1, and deliberately omits dictionaryVersion (legacy sets `Specified=false`) — exact match |
| getDeliveryOptions | Returns EDI sender account/password/URL from appSettings (:16) | POST /hiso/getDeliveryOptions | ✅ | Field-diffed 2026-07-27: `GetDeliveryOptionsResponseReturn` matches doc shape field-for-field, including legacy's never-populated messageID/recipientAccount (kept always-null for shape fidelity) |
| getData | Dynamic-mode data fill; static mode is an unimplemented stub in legacy (:17) | POST /hiso/getData | ✅ | Field-diffed 2026-07-27: stub-branch condition confirmed (`GetDataQuery.cs:63-69`). **Dynamic-mode data-fill pipeline live-verified 2026-07-28**: real session `0F456781-...` (practice 933) + `formInstanceOperationMode="N"` + real `patient.details` fields called against the running API → returned real live-DB values (NHI `ZZZ0083` — valid NZ NHI format, name `HAMZA ARSHAD`, DOB `1995-08-31`), confirming session→concept-dictionary→procedure-execution→XML-fill pipeline genuinely pulls from the live PMS DB, not a stub |
| saveContainer | Persists rendered form to DMS + ACC45 tables when completed=true (:18) | POST /hiso/saveContainer | ✅ | Field-diffed 2026-07-27: `SaveContainerCommand.cs` calls the real `IHisoDocumentHandler.AddDocumentAsync` and always also runs the ACC45 Detail/Diagnosis/Referral save regardless of `completed`, matching legacy |
| getFormView | Retrieves stored ACC45 form definition/view from DMS (:19) | POST /hiso/getFormView | ✅ | Field-diffed 2026-07-27: ported from real `Acc45DefinitionBuilder.GetACC45Definition`; resumePath/viewType/view/dataContainer are DB-backed matching doc shape |
| processAction | Dispatches on actionId; "addInvoice"/"launchForm" are no-op stubs in legacy (:20) | POST /hiso/processAction | ✅ | Field-diffed 2026-07-27: `ProcessActionCommand.cs:82-83` reproduces addInvoice/launchForm as genuine no-ops exactly matching legacy |
| Session expiry | Real HISO SessionGUID mechanism has NO expiry (confirmed via source per code comment) | ResolveHisoSessionQueryHandler | ✅ | Confirmed 2026-07-24/25: expiry logic intentionally removed from handler to match legacy; test suite updated to match |

## ERMS
| Operation | Legacy behavior (source) | Unified endpoint | Status | Notes |
|---|---|---|---|---|
| Ping | XML `<Ping>Success!</Ping>`, no auth (:12) | GET /erms/ping | ✅ | Present |
| Authenticate | Decodes/decrypts patientId+encounterId, can forward to Azure ERMS mirror (:13) | POST /erms/authenticate | ❌ | GAP confirmed 2026-07-27: `grep -rn "Azure"` across entire `src/` finds zero Azure-forwarding logic anywhere (only unrelated hits in secret-provider files) — the `EnableAzureERMSAPI`/practice-suffix-"azure"-substring proxy-to-`AzureEMRSAPI` behavior (EndpointInventory.md:36) is entirely unported. Practices whose EncounterId suffix contains "azure" would be served locally instead of being transparently proxied. Not fixed — reporting only per audit scope. |
| GetAccidents | (:14) | GET /erms/GetAccidents | ✅ | Present |
| GetClassifications | (:15) | GET /erms/GetClassifications | ✅ | Present |
| GetConsultNotes | Defaults min/max date window to last 24 months if not supplied — undocumented (BusinessRules.md BR-05) (:16) | GET /erms/GetConsultNotes | ✅ | Confirmed: `ErmsGetConsultNotesQueryHandler` (ErmsReadQueries.cs:213-225) explicitly defaults min=Now.AddMonths(-24)/max=Now when blank |
| GetCurrentUser | (:17) | GET /erms/GetCurrentUser | ✅ | Present |
| GetDischargeSummaryReportList | (:18) | GET /erms/GetDischargeSummaryReportList | ✅ | Present |
| GetDischargeSummaryDetails | (:19) | GET /erms/GetDischargeSummaryDetails | ✅ | Present |
| GetLaboratoryReportList | (:20) | GET /erms/GetLaboratoryReportList | ✅ | Present |
| GetLaboratoryReportDetails | Converts content via ConvertString2RTF (:21) | GET /erms/GetLaboratoryReportDetails | ✅ | Field-diffed 2026-07-27: real `ErmsRtfConverter.cs` (Adapters.Erms) implements the RTF conversion, wired into `ErmsDataRepository.cs` — matches legacy's `ConvertString2RTF` behavior |
| GetMedicalAllergies | (:22) | GET /erms/GetMedicalAllergies | ✅ | Present |
| GetNextOfKin | Root tag documented as `<Next_Of_Kin>`, code uses `<NextOfKin>` (doc gap, not necessarily a unified gap) (:23) | GET /erms/GetNextOfKin | ✅ | Present |
| GetPatientData | (:24) | GET /erms/GetPatientData | ✅ | Present |
| GetPatientMeasurement | (:25) | GET /erms/GetPatientMeasurement | ✅ | Present |
| GetPrescribedMedications | (:26) | GET /erms/GetPrescribedMedications | ✅ | Present |
| GetRegularMedications | (:27) | GET /erms/GetRegularMedications | ✅ | Present |
| GetRadiologyReportList | (:28) | GET /erms/GetRadiologyReportList | ✅ | Present |
| GetRadiologyReportDetails | (:29) | GET /erms/GetRadiologyReportDetails | ✅ | Present |
| GetRegisteredPractitioners | (:30) | GET /erms/GetRegisteredPractitioners | ✅ | Present |
| GetScannedList | (:31) | GET /erms/GetScannedList | ✅ | Present |
| GetScannedDetails | (:32) | GET /erms/GetScannedDetails | ✅ | Present |
| GetSmokingStatus | (:33) | GET /erms/GetSmokingStatus | ✅ | Present |
| SaveDocument | AWS vs legacy DMS path decided by AWSDoc.IndiciDMS.CheckAWSIsEnabled — undocumented (BusinessRules.md BR-09) (:34) | POST /erms/SaveDocument | ✅ | Fixed 2026-07-25: `ErmsWriteRepository.UpdateExistingDocumentAsync` now calls `IAwsDocumentService.CheckAwsIsEnabledAsync` (the real `AWSDocCore.dll`, already wired for HISO) and branches `[HSS].[uspUpdateExistingDoc_AWS]` vs `[HSS].[uspUpdateExistingDoc]`, matching `HSSDA.cs:38-64` exactly |

## KARO/HSS
| Operation | Legacy behavior (source) | Unified endpoint | Status | Notes |
|---|---|---|---|---|
| Ping | Always 200 `{"status":"success"}` (:17) | GET /karo/ping | ✅ | Present |
| Authenticate (GET) | Credentials in query string, logged in plaintext; undocumented in KARO_HSS_doc.md (:18) | GET /karo/authenticate | ✅ | Present |
| Authenticate (POST) | Matches documented `/authenticate` (:19) | POST /karo/authenticate | ✅ | Present |
| GetClinicalNotes | (:20) | GET /karo/clinicalnotes | ✅ | Present |
| GetConditions | (:21) | GET /karo/conditions | ✅ | Present |
| GetDemographics | Doc sample fields dateOfEnrolment/endEnrolmentDate map to DemographicInfo (:22) | GET /karo/demographics | ✅ | Present |
| GetDocuments | Branches on AWS-backed storage path (HSSDA.GetDocuments, not in doc) (:23) | GET /karo/documents | ✅ | Fixed 2026-07-25: `KaroDataRepository.GetDocumentsAsync` now takes `practiceSuffixNumeric`, calls `CheckAwsIsEnabledAsync` and branches to `[HSS].[uspGetDocuments_AWS]`; enriches ContentType per-row via `GetDocumentStatusFromIndiciAsync` when no identifier, or overwrites MessageData/MessageTitle/ContentType from `DocumentGetByDocumentKeyJsonResultAsync` for a single-doc lookup — matches `hsswebapi/DAL/South/HSSDA.cs:262-373` |
| GetEncounterSummary | Not documented; hardcoded JSON for identifier=diap/cvra, else {} (:24) | GET /karo/encountersummary | ✅ | Confirmed: KaroEncounterSummaryQueryHandler (KaroReadQueries.cs:229) replicates identical hardcoded diap/cvra JSON + `{}` fallback |
| GetLabResults | (:25) | GET /karo/labresults | ✅ | Present |
| GetMedications | (:26) | GET /karo/medications | ✅ | Present |
| GetObservations | (:27) | GET /karo/observations | ✅ | Present |
| GetProvider | (:28) | GET /karo/provider | ✅ | Present |
| GetRecallCategories | (:29) | GET /karo/recallcategories | ✅ | Present |
| GetRecalls | (:30) | GET /karo/recalls | ✅ | Present |
| GetScreeningCodes | (:31) | GET /karo/screeningcodes | ✅ | Present |
| GetPatientAttachment | Not documented; returns full attachment binary as base64 (:32) | GET /karo/patientattachment | ✅ | Present |
| SaveClinicalNotes | (:33) | POST /karo/clinicalnotes | ✅ | Present |
| SaveCondition | Response includes special "already exists" success message (:34) | POST /karo/conditions | ✅ | Confirmed: `KaroSaveConditionCommandHandler` (KaroWriteCommands.cs:82-85) returns success + "Diagnosis already exits against current Appointment." on the real `-5` sentinel |
| SaveSummary | Schema-driven via HSSDA.GetTemplateSchema, raw JObject parse (:35) | POST /karo/summary | ✅ | Present |
| SaveDocument | Saves to DMS via SaveToDMS() then HSSDA.InsertDocument (:37) | POST /karo/document | ✅ | Present |
| SaveInvoice | Returns service mapping ID or "Invoice already exists" (:38) | POST /karo/invoice | ✅ | Confirmed: `KaroSaveInvoiceCommandHandler` (KaroWriteCommands.cs:129-131) returns "Invoice already exits." on the real `-3` sentinel |
| SaveObservations | Requires at least one screening value or rejects (BusinessRules.md BR-14) (:39) | POST /karo/observations | ✅ | Confirmed: handler (KaroWriteCommands.cs:173-175) rejects with "Unable to Save Observation. Please Send at least one screening value..." when no values present |
| SaveRecall | (:40) | POST /karo/recalls | ✅ | Present |
| SaveScreeningCode | Legacy is a stub: logs payload, returns success without persisting or validating token (no InsertAndValidateToken call at all) (:41, BR-15) | POST /karo/screeningcodes | ✅ | Confirmed: `SaveScreeningCode() => Ok(status=success, message="")` (KaroCompatController.cs:186) — no-op, matches legacy stub exactly |

## COL
| Operation | Legacy behavior (source) | Unified endpoint | Status | Notes |
|---|---|---|---|---|
| Authenticate | Independent implementation from APIController.Authenticate; no Azure-forwarding logic (erms/EndpointInventory.md:42) | POST /erms/col/authenticate | ✅ | Present |
| GetCurrentPatientData | Not in ERMS_doc.md (:43) | GET /erms/col/GetCurrentPatientData | ✅ | Present |
| GetSessionData | Not in ERMS_doc.md (:44) | GET /erms/col/GetSessionData | ✅ | Present |
| GetProviderData | Not in ERMS_doc.md (:45) | GET /erms/col/GetProviderData | ✅ | Present |
| GetSurgeryData | Not in ERMS_doc.md (:46) | GET /erms/col/GetSurgeryData | ✅ | Present |
| GetDiagnosisData | Not in ERMS_doc.md (:47) | GET /erms/col/GetDiagnosisData | ✅ | Present |
| SaveInvoice | Financial write; not in ERMS_doc.md at all; serviceMappingId==-3 treated as "invoice already exists" idempotency signal (:48) | POST /erms/col/SaveInvoice | ✅ | Confirmed: `ColSaveInvoiceCommandHandler` (ColQueries.cs:234-239) treats `-3` as non-error (returns success, no "Invalid values passed!"), preserving legacy's duplicate sentinel on top of the platform's own natural-key idempotency check |

## Gaps found (summary)
- ❌ **ERMS `Authenticate`: Azure-forwarding proxy entirely unported.** Legacy transparently proxies to `AzureEMRSAPI` when `EnableAzureERMSAPI=="1"` AND the practice suffix parsed from EncounterId contains "azure" (undocumented, `Helpers/ERMSAPIProxy.cs`). No equivalent logic exists anywhere in the unified API's ERMS auth path. Impact: any practice relying on the Azure mirror would silently get served from the local/direct path instead. Not fixed — needs a decision (Zohaib) on whether to port the proxy or deliberately retire it, per the original doc's own recommendation.

(all other gaps below were closed 2026-07-25/26)

### Closed gaps (history)
- ~~ERMS `SaveDocument`: AWS-backed DMS branch not implemented~~ — Fixed: `ErmsWriteRepository.cs` now uses the real `IAwsDocumentService` (the same real `AWSDocCore.dll` already wired for HISO, contrary to the original assumption that it was non-portable) to branch `[HSS].[uspUpdateExistingDoc_AWS]` vs `[HSS].[uspUpdateExistingDoc]`.
- ~~KARO `GetDocuments`: AWS-backed storage branch not implemented~~ — Fixed: `KaroDataRepository.cs` now branches `[HSS].[uspGetDocuments_AWS]` vs `[HSS].[uspGetDocuments]` via the same real `IAwsDocumentService`, plus per-row/single-doc content enrichment matching `hsswebapi/DAL/South/HSSDA.cs:262-373`.

## Known pending items OUTSIDE this validator's endpoint-level scope (found during 2026-07-26 recheck)
These are not endpoint-presence or field-shape gaps (this validator's scope) — they're deeper,
already-tracked implementation gaps inside HISO's `saveContainer`/ACC45 flow, fully documented in
`hek_analysis/PROJECT_STATUS.md` (items 30/34). Listed here only so a parity recheck doesn't miss them:
- ~~Aspose HTML/image→PDF rendering~~ — **RESOLVED 2026-07-26**: the "no license available" framing
  was wrong (same mistake as the AWSDocCore gap) - Zohaib's real `Aspose.Words.dll` + `Aspose.Words.lic`
  were already sitting in `legacy-reference/Hiso/bin/`, just never checked. Verified working under
  .NET 8 via two scratch probes (license load + real HTML/image → PDF render, both produced valid `%PDF`
  bytes) with only `System.Text.Encoding.CodePages` + `System.Drawing.Common` as compat shims needed.
  Vendored to `src/Infrastructure/Legacy/Hiso/vendor/Aspose.Words.dll`+`.lic`; `AsposeMimeConverter`
  (real implementation, replaces the deleted `AsposeUnavailableMimeConverter` placeholder) wired into
  `IHisoMimeConverter`, which `HisoRequestEngine`'s `getData` attachment handling already called - no
  other call site needed changes. Only covers `getData`'s image/HTML attachment conversion - real
  legacy `saveContainer` (`FormSessionService.svc.cs:314-321`, `DocumentHandler.AddDocument`) does NOT
  call Aspose at all, it saves the raw `view` bytes to DMS as-is (see next item).
- ~~`Acc45Repository.SaveFormAsync` saved a JSON dump of `DataContainer` to DMS instead of the real
  `view` bytes~~ — **FIXED 2026-07-26**: now calls the same real `IHisoDocumentHandler` the SOAP
  `saveContainer` wire-compat path (`SaveContainerCommand`) already uses, passing `input.View`/
  `input.ViewType` through to `[dbo].[uspDocumentSave]` exactly like legacy's `DocumentHandler.AddDocument`
  (HTML saved as UTF8 text, PDF as base64-decoded bytes, extension resolved from `viewType`). Removed
  the now-unused `DmsDocumentService` JSON-placeholder dependency from `Acc45Repository`. Full solution
  build + all 4 test projects (23 tests) still pass.
- ~~`Acc45DetailRepository.BuildDiagnosisTable`/`BuildReferralTable` only built 1 column each~~ —
  **FIXED 2026-07-26**: this was never actually blocked on a live-DB schema (unlike the genuinely
  external AWSDoc/Aspose gaps) — the real, fixed column lists are hardcoded directly in the legacy
  source Zohaib already supplied (`legacy-reference/Hiso/Acc45DiagnosisBuilder.cs`'s `ACC45DiadColumns`
  array: 15 columns; `Acc45ReferralBuilder.cs`: 3 columns), just never read. Both builders now pre-seed
  the real fixed column set, seed the same per-row defaults legacy sets (IsActive/IsDeleted/Inserted-
  UpdatedBy/Inserted-UpdatedAt for diagnosis, UpdatedBy for referral, both from session `ProviderId`),
  and match diagnosis's dual group-matcher (`name="accident.diagnosis"` OR
  `conceptName="Patient_Accident_Diagnosis"`) - not just the single condition previously implemented.
  Full solution build + all 4 test projects (23 tests) still pass.

## ERMS/KARO/COL re-audit (2026-07-26, at Zohaib's request for a "tasali" recheck)
Re-scanned every real `AWSDoc.IndiciDMS.CheckAWSIsEnabled` call site across ERMS's and KARO's real
DAL source (`grep -n CheckAWSIsEnabled` on both `ermsapi/.../DAL/South/HSSDA.cs` and
`hsswebapi/.../DAL/South/HSSDA.cs`) to make sure no AWS branch was missed beyond the 2 already fixed.
Found 2 more real, previously-unported ERMS AWS branches (the `ErmsCompatController.cs` doc comments
even said "(non-AWS path)" - a self-flagged gap that had gone unactioned):
- ~~ERMS `GetOtherDocs`~~ (`HSSDA.cs:653-726`, backs both `GetDischargeSummaryReportList` and
  `GetScannedList`) — **FIXED**: `ErmsDataRepository.GetOtherDocsAsync` now branches
  `[HSS].[uspGetOtherDocs_AWS]` vs `[HSS].[uspGetOtherDocs]` via `CheckAwsIsEnabledAsync`, enriching
  each row's `DataType` via `GetDocumentStatusFromIndiciAsync` when AWS-enabled.
- ~~ERMS `GetDocResults`~~ (`HSSDA.cs:260-345`, backs both `GetDischargeSummaryDetails` and
  `GetScannedDetails`) — **FIXED**: `ErmsDataRepository.GetDocResultsAsync` now branches
  `[HSS].[uspGetDocResults_AWS]` vs `[HSS].[uspGetDocResults]`, overwriting `Content`/`DocumentId`/
  `DataType` on the first row via `DocumentGetByDocumentKeyJsonResultAsync` when a `referenceId` is
  given and AWS is enabled.
Both needed threading `practiceSuffixNumeric` through `ErmsReadPipeline`'s already-available
`rawSecondSegment` (previously discarded as `_` at all 4 call sites) - same pattern as KARO's fix.
Confirmed via source `grep` that this closes out ALL 3 real ERMS `CheckAWSIsEnabled` call sites
(`UpdateExistingDocument`/`GetOtherDocs`/`GetDocResults`) and the 1 real KARO one (`GetDocuments`) -
none remain unaccounted for. COL has no AWS involvement at all (confirmed via `grep` - `COLController.cs`
and the COL portion of `APIController.cs` never reference `AWSDoc`). Full solution build + all 4 test
projects (23 tests) still pass.

## DMSProxy path (investigated 2026-07-26, not a gap)
`IHisoDocumentHandler`'s remarks flagged legacy's `AddDirectDMS=0` branch (external `DMSProxy` SOAP
client) as unported. Investigated: `DMSProxy.dll` is a thin ASMX/SOAP client wrapper, not a
self-contained library like AWSDocCore/Aspose - its real logic lives on a private-LAN server
(`Web.config`: `DMSServiceURL=http://192.168.0.157/DMSService/ClientService.asmx`, a specific
clinic's internal IP, unreachable from anywhere outside that LAN). Critically, legacy's own
`Web.config` has `AddDirectDMS=1` - meaning legacy itself never takes the DMSProxy branch in
production; it's a dead/unused code path in the real system too (same class of finding as the
already-known dormant DAL modules). The unified API's direct-DB-only implementation therefore already
matches legacy's real, live behavior exactly - not a gap.

## Field-level diff pass (2026-07-27)
Resumed only the rows previously marked "present but not field-diffed": all 6 HISO rows
(getVersion/getDeliveryOptions/getData/saveContainer/getFormView/processAction), ERMS
`Authenticate` (Azure-forward), and ERMS `GetLaboratoryReportDetails` (RTF conversion).
5 of 8 confirmed exact matches via source grep (no live call needed — code was unambiguous).
1 new gap found: **ERMS Authenticate Azure-forwarding proxy is completely unported** (see Gaps
found above) — this was previously mis-classified as "just needs diffing" when it's actually a
missing feature.

Also checked (at Zohaib's request) the Acc45 TVP-on-empty-XML concern from
`hek_analysis/v1.1-plan-status.md` Step 4: **already resolved**, as a side effect of the
2026-07-26 `BuildDiagnosisTable`/`BuildReferralTable` column fix — both now pre-seed their full
fixed column set before scanning the XML, so when no matching `<group>` nodes are found they
return a properly-shaped zero-row `DataTable` (a valid empty TVP), not a crash. Verified in
`Acc45DetailRepository.cs:161-173`. No code change needed.

## Last updated
2026-07-27 — field-level diff pass on all previously-unverified rows; found 1 new gap (ERMS Azure-forward, unported); confirmed Acc45 empty-XML TVP concern already resolved by an earlier fix. See "Field-level diff pass (2026-07-27)" section above.

2026-07-26 — recheck: rebuilt + reran full test suite (23/23 pass, 0 warnings/errors), re-scanned entire
`src/` for TODO/stub/deferred/not-implemented markers. Confirmed the two AWS-branch fixes from 2026-07-25
are solid and nothing else regressed. Found and fixed one stale doc-comment (`KaroCompatController.cs`
still said "AWS branch deferred" after the fix landed). Surfaced 2 real, still-open items that are outside
this validator's endpoint-parity scope (see section above) — both already tracked in PROJECT_STATUS.md,
both blocked on external dependencies (Aspose license, real UDT column lists), not silent gaps.

2026-07-25 — full pass complete, 0 gaps remaining. All 60 legacy operations (HISO 6, ERMS 23, KARO 24, COL 7) confirmed present and matching. The 2 AWS-branch gaps found in the first pass were corrected: they were NOT actually blocked by a non-portable DLL (that assumption, baked into old code comments, was stale) — HISO already had a real, working `IAwsDocumentService` implementation against the real `AWSDocCore.dll` Zohaib supplied. Reused that same service for ERMS `SaveDocument`'s `UpdateExistingDocumentAsync` and KARO's `GetDocumentsAsync`, both now branching AWS vs non-AWS exactly like their legacy DAL sources. Full solution build + all 4 test projects (23 tests total) still pass after the change.
