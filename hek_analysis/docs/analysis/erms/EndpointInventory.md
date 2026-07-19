# ERMS Web API — Endpoint Inventory

## Summary
30 HTTP actions were found across two controllers: 23 in `APIController` (XML, HISO-concept data for ERMS eReferrals) and 7 in `COLController` (JSON, Pegasus/"COL" claiming-adjacent data). Routing is `{controller}/{action}/{id}` (lower-cased), so `APIController.GetPatientData` resolves to `GET /api/getpatientdata` and `COLController.GetSurgeryData` resolves to `GET /col/getsurgerydata`. All actions authenticate via a Bearer-style header token re-validated per request against `HSSDA.InsertAndValidateToken`, except `Ping` and `Authenticate` themselves.

## Findings

### APIController (`/api/...`) — XML, `[EnableCors]` commented out

| Method | Route (action) | Action (C#) | Request | Response | Auth Required | Notes |
|---|---|---|---|---|---|---|
| GET | `/api/ping` | `Ping()` | none | XML `<Ping>Success!</Ping>` | No | Healthcheck only |
| POST | `/api/authenticate` | `Authenticate()` | XML `Credential` (username, password, patientId, encounterId) | XML `<Authentication>` (Token, Expiry, PracticeId) or `<Error>` | No (issues the token) | Decodes/decrypts patientId+encounterId first; can forward to Azure ERMS mirror (see below) |
| GET | `/api/getaccidents` | `GetAccidents(pmsPatientId, pmsEncounterId, pmsOrder="desc", pmsMinDateTime="", pmsMaxDateTime="")` | query string | XML `<Accidents>` | Yes | Documented in ERMS_doc.md |
| GET | `/api/getclassifications` | `GetClassifications(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime)` | query string | XML `<Problems>` | Yes | Documented (as "Problems/Classifications") |
| GET | `/api/getconsultnotes` | `GetConsultNotes(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime)` | query string | XML `<ConsultNotes>` | Yes | Defaults min/max date window to last 24 months if not supplied (undocumented — see BusinessRules.md BR-05) |
| GET | `/api/getcurrentuser` | `GetCurrentUser(pmsPatientId, pmsEncounterId, LocationId="", pmsUserId="")` | query string | XML `<CurrentUser>` | Yes | Documented |
| GET | `/api/getdischargesummaryreportlist` | `GetDischargeSummaryReportList(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime)` | query string | XML `<DischargeReports>` | Yes | Documented |
| GET | `/api/getdischargesummarydetails` | `GetDischargeSummaryDetails(pmsPatientId, pmsEncounterId, pmsReferenceId)` | query string | XML `<DischargeSummaryContents>` | Yes | Documented |
| GET | `/api/getlaboratoryreportlist` | `GetLaboratoryReportList(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime)` | query string | XML `<LaboratoryReports>` | Yes | Documented |
| GET | `/api/getlaboratoryreportdetails` | `GetLaboratoryReportDetails(pmsPatientId, pmsEncounterId, pmsReferenceId)` | query string | XML `<LaboratoryReportsContent>` | Yes | Documented; converts content via `ConvertString2RTF` |
| GET | `/api/getmedicalallergies` | `GetMedicalAllergies(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime)` | query string | XML `<MedicalWarnings>` | Yes | Documented |
| GET | `/api/getnextofkin` | `GetNextOfKin(pmsPatientId, pmsEncounterId)` | query string | XML `<NextOfKin>` (root tag documented as `<Next_Of_Kin>` — see DocumentationGap.md) | Yes | Documented |
| GET | `/api/getpatientdata` | `GetPatientData(pmsPatientId, pmsEncounterId)` | query string | XML `<PatientData>` | Yes | Documented |
| GET | `/api/getpatientmeasurement` | `GetPatientMeasurement(pmsPatientId, pmsEncounterId)` | query string | XML `<Measurement>` | Yes | Documented |
| GET | `/api/getprescribedmedications` | `GetPrescribedMedications(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime)` | query string | XML `<PrescribedMedications>` | Yes | Documented |
| GET | `/api/getregularmedications` | `GetRegularMedications(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime)` | query string | XML `<RegularMedications>` | Yes | Documented |
| GET | `/api/getradiologyreportlist` | `GetRadiologyReportList(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime)` | query string | XML `<RadiologyReports>` | Yes | Documented |
| GET | `/api/getradiologyreportdetails` | `GetRadiologyReportDetails(pmsPatientId, pmsEncounterId, pmsReferenceId)` | query string | XML `<RadiologyReportContents>` | Yes | Documented |
| GET | `/api/getregisteredpractitioners` | `GetRegisteredPractitioners(pmsPatientId, pmsEncounterId, pmsLocationId="")` | query string | XML `<RegisteredPractitioners>` | Yes | Documented |
| GET | `/api/getscannedlist` | `GetScannedList(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime)` | query string | XML `<ScanDocumentReports>` | Yes | Documented |
| GET | `/api/getscanneddetails` | `GetScannedDetails(pmsPatientId, pmsEncounterId, pmsReferenceId)` | query string | XML `<ScanReportContent>` | Yes | Documented |
| GET | `/api/getsmokingstatus` | `GetSmokingStatus(pmsPatientId, pmsEncounterId)` | query string | XML `<SmokingStatus>` | Yes | Documented |
| POST | `/api/savedocument` | `SaveDocument()` | XML `ReferralDocument` (base64 content) | XML `ReferralDocument` (content stripped) or HTTP 400 | Yes | Documented; writes to DMS (`HSSDA.DocumentSave`) and inserts a document record (`HSSDA.InsertDocument`); AWS vs. legacy DMS path decided by `AWSDoc.IndiciDMS.CheckAWSIsEnabled` (undocumented — see BusinessRules.md BR-09) |

**Azure-forwarding behavior (undocumented, all GET/POST actions above except `Ping`):** if `AppSettings["EnableAzureERMSAPI"] == "1"` **and** the practice-id suffix parsed from `EncounterId` contains the substring `"azure"`, the request is transparently proxied to `AppSettings["AzureEMRSAPI"]` (`Helpers/ERMSAPIProxy.cs`) instead of being served locally. This is not mentioned anywhere in `ERMS_doc.md`.

### COLController (`/col/...`) — JSON, `[EnableCors(origins: "*", headers: "*", methods: "*")]` (wildcard, always on)

| Method | Route (action) | Action (C#) | Request | Response | Auth Required | Notes |
|---|---|---|---|---|---|---|
| POST | `/col/authenticate` | `Authenticate()` | JSON `Credential` | JSON `{Token, Expiry, PracticeId, error}` | No (issues token) | Independent implementation from `APIController.Authenticate`; no Azure-forwarding logic |
| GET | `/col/getcurrentpatientdata` | `GetCurrentPatientData(pmsPatientId, pmsEncounterId)` | query string | JSON `PegasusAPIModel.PatientData` | Yes | Not in ERMS_doc.md |
| GET | `/col/getsessiondata` | `GetSessionData(pmsPatientId, pmsEncounterId)` | query string | JSON `PegasusAPIModel.SessionData` | Yes | Not in ERMS_doc.md |
| GET | `/col/getproviderdata` | `GetProviderData(pmsPatientId, pmsEncounterId)` | query string | JSON `PegasusAPIModel.ProviderData` | Yes | Not in ERMS_doc.md |
| GET | `/col/getsurgerydata` | `GetSurgeryData(pmsPatientId, pmsEncounterId, LocationId="")` | query string | JSON `PegasusAPIModel.SurgeryData` | Yes | Not in ERMS_doc.md |
| GET | `/col/getdiagnosisdata` | `GetDiagnosisData(pmsPatientId, pmsEncounterId, pmsOrder="desc", pmsMinDateTime="", pmsMaxDateTime="")` | query string | JSON `PegasusAPIModel.DiagnosisData` | Yes | Not in ERMS_doc.md |
| POST | `/col/saveinvoice` | `SaveInvoice()` | JSON `SaveInvoice` (ServiceCode, ServiceName, AmountInclGST, Payee, ServiceProvider, ServiceDate, PegasusReference, ClaimShortCode, ...) | JSON `{status, message}` | Yes | **Financial write endpoint**, not in ERMS_doc.md at all — creates/updates a billing service record via `HSSDA.InsertUpdateService`; treats `serviceMappingId == -3` as "invoice already exists" (idempotency signal) |

## Evidence
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Controllers\APIController.cs` (all `[AcceptVerbs]`-decorated methods, lines 26-1899)
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Controllers\COLController.cs` (lines 23-462)
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\App_Start\WebApiConfig.cs` (route template)
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Helpers\ERMSAPIProxy.cs` (Azure forwarding)
- `E:\claude_projects\hek_analysis\docs\_source_docs\ERMS_doc.md`

## Risks
- 7 of 30 endpoints (all of `COLController`, including a write/financial one) are completely undocumented externally; migrating without validating these against the actual Pegasus/COL consumer risks breaking an integration nobody on the migration team may know still exists.
- The Azure-forwarding proxy path is a silent request-routing decision based on a substring match inside a decrypted ID — a subtle, easy-to-miss behavior to reproduce or intentionally drop during migration.
- `[EnableCors(origins: "*")]` on a controller containing a financial write (`SaveInvoice`) is inconsistent with the read-only `APIController`'s (commented-out) CORS policy — inconsistent trust boundaries between the two controllers.

## Recommendations
- Confirm with the client whether the COL/Pegasus consumer is still active before deciding to carry `COLController`'s 7 endpoints into the unified platform.
- Document and then either preserve or deliberately retire the Azure-forwarding mechanism as part of the redesign; do not let it silently disappear.
- See DocumentationGap.md for the full list of discrepancies between `ERMS_doc.md` and the implementation.
