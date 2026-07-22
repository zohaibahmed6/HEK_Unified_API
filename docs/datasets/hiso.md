# HISO Field Inventory

Source: `src/Adapters.Hiso/*` (GetData/GetVersion/GetDeliveryOptions/ProcessAction/SaveContainer/GetFormView),
`src/Infrastructure/Legacy/Demographics/DemographicsRepository.cs` (`GetHisoAsync`),
`src/Contracts/Demographics/DemographicsHiso.cs`. HISO is concept-driven: fields are resolved at
runtime through a concept dictionary (`[Hiso].[UspGetHisoConcepts]`) rather than fixed DB columns.

## Canonical demographics mapping (`DemographicsHiso`) — CONFIRMED live
Confirmed 2026-07-22 against real production concept resolution (see `DemographicsRepository.cs`
doc comment and `PROJECT_STATUS.md`), replacing an earlier guessed stored-procedure name.

| Field | Type | Concept name | Notes |
|---|---|---|---|
| PatientId | int | — (route param) | |
| PracticeId | string | — (session) | |
| FirstName | string | `Patient_FirstName` | |
| LastName | string | `Patient_Surname` | not "LastName" — real concept name |
| DateOfBirth | DateOnly | `Patient_DateOfBirth` | |

Resolved via `IHisoConceptDictionary` + `IHisoRequestEngine` + `IHisoConceptExecutor` to the real
`Hiso.uspGetPatient` procedure — the procedure name is never hardcoded in the repository.

## `getData` request/response envelope — INFERRED from real WCF contract, not DB-verified
`GetDataRequest(SessionKey: Guid, DataContainer: GetDataFormData)`
`GetDataFormData(FormMetaData: GetDataFormMetaData, SubmittedDataXml: string?)`
`GetDataFormMetaData`: FormInstanceId, FormInstanceVersion, FormEngineId, FormInstanceOperationMode
("N" = new form is the only real logic path), FormDefinitionId, FormDefinitionVersion, FormDefinitionTitle.

The actual field/section content is carried as raw form XML (`SubmittedDataXml`) matching legacy's
concept/section structure exactly — not re-modeled as JSON, since the concept-mapping engine parses
that XML directly.

## Other HISO operations (GetVersion, GetDeliveryOptions, ProcessAction, SaveContainer, GetFormView)
Request/response DTOs exist as thin ports of the legacy SOAP contract
(`src/Adapters.Hiso/{GetVersion,GetDeliveryOptions,ProcessAction,SaveContainer,GetFormView}/*.cs`).
Not field-inventoried here in detail — these are session/action envelopes (sessionKey, form
instance ids, action payloads), not patient-data field sets; see those files directly for exact
shapes if needed.

## Session
`HisoSessionResolver` / `IHisoSessionRepository` resolve a `HealthLinkSession` keyed by
`[Appointment].[tblHealthLinkSession]` (patientId, encounterId, practiceId), with a 12-hour expiry
enforced in `ResolveHisoSessionQueryHandler` — confirmed real table per `PROJECT_STATUS.md` Block 1.
