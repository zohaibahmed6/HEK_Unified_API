# ERMS (+ COL/Pegasus) Field Inventory

Source: `src/Adapters.Erms/*`, `src/Contracts/Demographics/DemographicsErms.cs` /
`DemographicsCol.cs`, `src/Infrastructure/Legacy/Demographics/DemographicsRepository.cs`
(`GetErmsAsync`, `GetColAsync`).

## ERMS demographics (`DemographicsErms`) — CONFIRMED live against real data
Confirmed 2026-07-22, patient 2459731, same real `[HSS].[uspGetDemographics]` procedure KARO uses
(identical result-set shape, confirmed by direct `sqlcmd` inspection):

| Field | Type | Real DB column | Notes |
|---|---|---|---|
| PatientId | int | — (param) | |
| EncounterId | int | 0 (hardcoded in this mapping) | not derived from the row |
| FirstName | string | `Given` | the `FirstName` column exists but holds the same unusable composite-reference garbage documented for KARO |
| LastName | string | `Family` | same caveat |
| Dob | DateOnly | `BirthDate` | |
| Nhi | string? | `NHI` | confirmed value `ZZZ0121` for patient 2459731 |

Routed via `IErmsDemographicsRepository.GetDemographicsAsync(practiceSuffix, patientId)` — flagged
assumption: the canonical JWT's `practiceId` claim is passed through directly as ERMS's own
`practiceSuffix`, which works today only because `TEST-PRACTICE-001` is valid on both sides — not a
confirmed general mapping.

## ERMS `GetPatientData` wire shape (`ErmsDataTableMapper`/`PatientData`) — separate from the above
The XML wire contract for `/erms/GetPatientData` uses different property names
(`FirstName`/`Surname`/`DateOfBirth`/`PatientNHI`) mapped from pipe-delimited DataTable columns —
this is the shape returned to legacy ERMS callers and is NOT the same clean values used in the
canonical mapping above (deliberately different mapping paths for two different contracts).

## ERMS Auth (`ErmsCredential`, XML) — CONFIRMED from `ERMS_doc.md`
`<Credential><Username/><Password/><PatientId/><EncounterId/></Credential>`

## ERMS read-model envelopes (`ErmsReadModels.cs`) — ported from real `APIModels.cs`
Representative envelopes and their fields (all XML-attributed, legacy element names preserved):

| Envelope | Item fields |
|---|---|
| `CurrentUser` | FirstName, Surname, Middlename, FullName, RegisteringBody, RegistrationNumber, PersonalHPI, ApplicationUserID, FacilityHPI, HealthlinkEDI, PMSID |
| `Problems` (conditions) | Comments, Description, DateOfOnset, Code, CodingSystem, DateRecorded, IsLongTerm (+ order/minDateTime/maxDateTime/conceptID/referenceID attrs) |
| `ConsultNotes` | ConsultDate, ConsultExam, ConsultHistory, ConsultAssess, ConsultPlan |
| `NextOfKin` (`PatientNOK`) | Address (AdditionalLine/City/Postcode/StreetName/StreetNumber/Suburb), Firstname, Middlename, Surname, Mobile, PreferredNumber, Relationship, ResidentialPhone, WorkPhone, IsDefault |
| `MedicalWarnings` | Comments, Date, Description, RecordedByID |
| `RegisteredPractitioners` | Title, FirstName, Surname, FullName, RegistrationNumber, RegisteringBody, address fields, Phone, Fax, FacilityHPI, HealthLinkEDI, PMSID, Email, PersonalHPI |
| `SmokingStatus` | ConsumptionDescription, Code, CodingSystem, Date |
| `Accidents` | RegistrationNumber, Date, DiagnosisDescription, IsWorkRelated, LocationDescription |
| `Measurement` | BPSYS, BPDIA, Weight, Height, BMI |

Not exhaustively listed here: `ErmsReportModels.cs` (lab/radiology/discharge/scanned report
list+detail shapes), `ErmsReferralDocument.cs` (SaveDocument body incl. the real `PatiendID`
typo). See those files directly for full field lists if needed — not reproduced here to avoid
transcription drift from the real code.

## COL / Pegasus (`ColModels.cs`) — mostly UNCONFIRMED / inferred
COL is explicitly undocumented per the SRS; `GetColAsync`'s procedure
(`[HSS].[uspGetCurrentPatientData]`) is flagged as "still an unconfirmed inference," out of scope
for the 2026-07-22 fix session.

`ColPatientData` (lowercase field names = real legacy DataTable columns / JSON property names,
Newtonsoft default, no camel-casing): id, lastname, firstname, title, home/postal address
(street/suburb/city/postcode ×2), dayphone, ahphone, cellphone, dob, nhinumber, gmscode,
csccardnnumber, cscstartdate, cscexpiredate, iscscholder, huccode, huccardnnumber, hucstartdate,
hucexpiresdate, ishucholder, gendercode, ethnicitycode1-6 (+desc), quintile, genderdesc, email,
enrolmentcode, nok* (name/residence/street/suburb/city/phone/ahphone/relationship), isenrolled,
iscapitated.

`DemographicsCol` (canonical mapping, CONFIRMED shape only, not confirmed against live DB):
PatientId, PracticeId, FirstName, LastName — mapped from `row["FirstName"]`/`row["LastName"]`
directly (not through `Given`/`Family` like KARO/ERMS — unconfirmed whether COL's `FirstName`/
`LastName` columns have the same composite-garbage problem documented for KARO/ERMS).

Other COL shapes: `ColSessionData`, `ColProviderData`, `ColDiagnosisData`, `ColSurgeryData`,
`ColUserAuthenticate` (Token/Expiry/PracticeId/error), `ColSaveInvoiceRequest` (ServiceCode,
ServiceName, AmountInclGST, Description, AccountHolderID, Payee, ServiceProvider,
ServiceProviderType, ServiceDate, PegasusReference, EncounterID, PatientID, Userid, ClaimShortCode).
