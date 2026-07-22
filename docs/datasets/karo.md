# KARO / HSS Field Inventory

Source: `src/Adapters.Karo/*`, `src/Contracts/Demographics/DemographicsKaro.cs`,
`src/Infrastructure/Legacy/Demographics/DemographicsRepository.cs` (`GetKaroAsync`).

## Demographics (`DemographicsKaro`) — CONFIRMED live against real data
Confirmed 2026-07-20 against real `PMS_NZ_V2` data, patient 2459731 (`PROJECT_STATUS.md` open item 28).
`[HSS].[uspGetDemographics]` result columns actually holding usable values:

| Field | Type | Real DB column | Notes |
|---|---|---|---|
| PatientId | int | — (param `@pPatientID`) | |
| PracticeId | string | — (routing context) | |
| FirstName | string | `Given` | **not** `FirstName` — that column exists but holds unrelated composite reference data (e.g. `554:1000111/1000310/1\|&\|LnB`) |
| LastName | string | `Family` | same caveat as above for `LastName` column |
| DateOfBirth | DateOnly | `BirthDate` | |
| DateOfEnrolment | DateOnly? | `DateOfEnrolment` | empty string (not DBNull) when unset — handled by `ParseOptionalDate` |
| EndEnrolmentDate | DateOnly? | `EndEnrolmentDate` | same empty-string handling |

## Auth (`HssAuthenticateRequest`) — CONFIRMED from `KARO_HSS_doc.md`
| Field | Type | Notes |
|---|---|---|
| Username | string | |
| Password | string | |
| PatientId | string? | |
| EncounterId | string? | |
| System (JSON: `system`) | string? | |
| Pho (JSON: `pho`) | string? | |

Bindable via both GET query string and POST JSON body (KARO-BR-07).

## Write-operation DTOs (`KaroWriteRequests.cs`) — ported from real `Models/APIModels.cs`
| DTO | Fields |
|---|---|
| `KaroConsultNoteRequest` | PatientId, EncounterId, UserId, SubjectiveNotes, ObjectiveNotes, Assessment, Plans, AppointmentAdvice, Date |
| `KaroConditionRequest` | PatientId, EncounterId, UserId, DiagnosisDate, ConceptId, Name, FSN, Type, OnSetDate, Summary, IsLongTerm |
| `KaroInvoiceRequest` | PatientId, EncounterId, UserId, LocationId, Name, Code, ClaimType, Fee, payee |
| `KaroObservationsRequest` | PatientId, EncounterId, UserId, Temperature, WaistCircumference, Height, Weight, BPSys, BPDia, HeartRate, Risk, Framingham, Notes |
| `KaroRecallRequest` | PatientId, EncounterId, UserId, Group, CategoryId, Priority, DueDate, Notes, Reason |
| `KaroDocumentRequest` | PatientId, EncounterId, MessageSubject, MessageData (byte[]), ContentType, ItemType |

## Read-side response envelope (`KaroRootResponse<T>`)
`(PatientId: string?, ResourceType: string, System: string, Entry: List<T>)` — legacy
`Root<Demographic>("Patient", "hss")` shape, camelCase JSON via `PrepareJSON`.
`KaroDemographic(ListDemographicInfo: List<KaroDemographicInfo>, Listcardtype: List<KaroCardInfo>)`
— `KaroDemographicInfo`/`KaroCardInfo` element shapes not separately inventoried here; see
`src/Adapters.Karo/Demographics/` if needed.

## 21 KARO endpoints (routes only — see `docs/architecture.md` for the full list)
ping, authenticate (GET+POST), demographics, clinicalnotes, conditions, documents, labresults,
medications, observations, provider, recallcategories, encountersummary, recalls, screeningcodes,
patientattachment, + POST variants (clinicalnotes, conditions, invoice, observations, recalls,
screeningcodes, document, summary).
