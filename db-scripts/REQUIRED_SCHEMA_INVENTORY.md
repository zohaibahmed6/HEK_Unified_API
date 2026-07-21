# Required Legacy-Shaped Schema Inventory

**Purpose:** a complete, code-derived catalog of every table and stored procedure Block 2's
Infrastructure repositories call, with exact parameter names and result columns. This is a
planning document, not a script — nothing here has been run against any database. It exists so
that when we build a **new, dedicated database** for HEK Core API (not `PMS_NZ_V2`, not
`ClinicBooking` — a fresh one we own), we have one authoritative source for exactly what needs to
exist, instead of re-deriving it from source each time.

**How this was produced:** extracted directly from `src/Infrastructure/Legacy/**/*.cs` — every
`CommandType.StoredProcedure` call, every `SqlParameter` name, and every `row["ColumnName"]` read.
This reflects what the *code* expects, not a confirmed real legacy schema (see
`hek_analysis/PROJECT_STATUS.md` §3 item 28 — no live schema access has ever been available in
this project; every name here is this project's own invented-but-consistent contract, which a
fresh dedicated database can simply satisfy exactly).

**Status:** not yet built anywhere. See `AI_USAGE_LOG.md` for the decision trail (tried
`PMS_NZ_V2` — no DDL rights and a different real schema anyway; tried `ClinicBooking` — turned out
to be an unrelated existing app's database; user decided: catalog first, build fresh later).

---

## Schemas referenced

| Schema | Used by |
|---|---|
| `Hiso` | HISO's concept-mapping engine (`HisoConceptExecutor`) + ACC45-specific procedures |
| `HSS` | KARO/ERMS/COL-sourced domain repositories (confirmed real schema name — `uspGetDemographics` appears in `PROJECT_STATUS.md` §2 from direct source inspection) |
| `Task` | Dormant `DMSDA.cs` port only (`TaskPathLabInsert`) |
| `Profile` | Dormant `DMSDA.cs` port only (`GetOrganizationByEDI`) |
| `dbo` | Dormant `DMSDA.cs` port (DMS-related) + `Appointment.tblHealthLinkSession` (HISO session, Block 1) |
| `Appointment` | HISO session table (`tblHealthLinkSession`) - confirmed columns only (HISO-BR-01) |

---

## Foundation: HISO concept-mapping engine (`Hiso` schema)

### `Hiso.USPGetProcedureParamList` — the parameter dictionary itself (HISO-BR-03)
- **Params:** `@pProcedureName` (nvarchar)
- **Returns:** `Parameter_name` (nvarchar) — one row per parameter the named procedure expects.
  `HisoConceptExecutor` maps session/request values onto whichever parameter names come back
  (`@patientid`/`@ppatientid`, `@providerid`/`@pproviderid`, `@appointmentid`/`@pappointmentid`,
  `@practiceid`/`@ppracticeid`, `@acc45id`/`@pacc45id`, `@ppracticelocationid`, `@fromdate`,
  `@todate`, `@startrowindex`, `@maximumrows`, `@search`, `@sortby`, `@minvalue`, `@maxvalue`,
  `@pcode`/`@vcode`, `@referenceid`) — so this table needs one row per (procedure, parameter-name)
  pair for every `Hiso.*` procedure below that the engine calls.
- **Backing table suggestion:** `Hiso.ProcedureParams (ProcedureName nvarchar(128), Parameter_name nvarchar(128))`

### HISO domain-read procedures (executed dynamically via the engine above)
| Procedure | Called by | Result columns |
|---|---|---|
| `Hiso.uspGetPatient_Demographics` | Demographics (HISO) | `FirstName`, `LastName`, `DateOfBirth` |
| `Hiso.uspGetPatient_ConsultNotes` | Clinical Notes (HISO) | `NoteId`, `Author`, `CreatedAt`, `Content` |
| `Hiso.uspGetPatient_Diagnosis` | Conditions (HISO) | `ConditionId`, `DiagnosisCode`, `Description`, `IsLongTerm`, `SideCode`, `SideDescription` |
| `Hiso.uspGetPatient_Medications` | Medications (HISO) | `MedicationId`, `Name`, `PrescribedDate` |
| `Hiso.uspGetPatient_Measurements` | Observations (HISO) | `ObservationId`, `ConceptId`, `Value`, `RecordedAt` |
| `Hiso.uspGetPatient_LaboratoryReport` | Lab Results (HISO) | `ReportId`, `Type`, `Date` |
| `Hiso.uspGetPatient_Acc45Form` | ACC45 forms, dynamic mode | any columns (mapped generically into a dictionary) |
| `Hiso.uspGetPatient_Acc45Form_Static` | ACC45 forms, static mode | any columns |

**Referenced but not actually invoked by any live Block 2 endpoint** (present only in
`HisoConceptExecutor`'s AWS-eligibility check, which logs a warning and falls through to the
normal path rather than branching - so these never get called): `Hiso.uspGetPatient_Attachment`,
`Hiso.uspGetPatient_IncomingLetter`, `Hiso.uspGetPatient_OutgoingLetter`,
`Hiso.uspGetPatient_OutgoingLetter_Author`, `Hiso.uspGetPatient_RadiologyReport`. Not required for
a working database, but harmless to stub if completeness is wanted later.

### ACC45-specific procedures (`Hiso` schema, called directly, not through the concept engine)
| Procedure | Params | Result / Output |
|---|---|---|
| `Hiso.uspGetDeliveryOptions` | `@pPracticeID` | `Url`, `PracticeEdi` |
| `Hiso.uspGetFormView` | `@pFormInstanceID` | `ViewType`, `View`, `DataContainer` (nvarchar/JSON) |
| `Hiso.uspSaveAcc45Definition` | `@pFormInstanceID`, `@pPatientID`, `@pAppointmentID`, `@pPracticeID`, `@pDataContainer`, `@pView`, `@pViewType`, `@pCompleted`, `@pDmsGuid` | write-only, no output param |
| `Hiso.uspProcessAction_Save` | `@pSessionKey`, `@pPatientID`, `@pAppointmentID`, `@pPracticeID`, `@pActionContainer` | write-only |
| `Hiso.uspProcessAction_AddTask` | same as above | write-only |
| `Hiso.uspProcessAction_AddInvoice` | same as above | write-only |
| `Hiso.uspProcessAction_LaunchForm` | same as above | write-only |

### Task procedures (`Hiso` schema)
| Procedure | Params | Result / Output |
|---|---|---|
| `Hiso.uspGetConceptName` | `@pConceptCode` | `ConceptName` |
| `Hiso.uspAddTask` | `@pPatientID`, `@pSubject`, `@pStatusID` | output `@pTaskIDOut` |

---

## `HSS` schema (KARO / ERMS / COL)

### Demographics
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetDemographics]` (confirmed real name) | `@pPatientID` | `FirstName`, `LastName`, `DateOfBirth`, `DateOfEnrolment`, `EndEnrolmentDate` |
| `[HSS].[uspGetPatientData]` | `@pPatientID` | `EncounterId`, `FirstName`, `LastName`, `Dob`, `Nhi` |
| `[HSS].[uspGetCurrentPatientData]` | `@pPatientID` | `FirstName`, `LastName` |

### Clinical Notes
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetClinicalNotes]` | `@pPatientID`, `@pEncounterID`, `@pSinceDate`, `@pUntilDate`, `@pSortOrder` | `NoteId`, `Author`, `CreatedAt`, `Content` |
| `[HSS].[uspGetConsultNotes]` | same | same |
| `[HSS].[uspSaveClinicalNotes]` | `@pPatientID`, `@pEncounterID`, `@pContent` | output `@pNoteIDOut` |

### Conditions
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetConditions]` | `@pPatientID`, `@pEncounterID` | `ConditionId`, `DiagnosisCode`, `Description`, `IsLongTerm`, `SideCode`, `SideDescription` |
| `[HSS].[uspGetClassifications]` | same | same |
| `[HSS].[uspGetDiagnosisData]` | same | same |
| `[HSS].[uspFindConditionByNaturalKey]` | `@pEncounterID`, `@pDiagnosisCode` | same |
| `[HSS].[uspSaveCondition]` | `@pPatientID`, `@pEncounterID`, `@pDiagnosisCode`, `@pDescription`, `@pIsLongTerm`, `@pSideCode`, `@pSideDescription` | output `@pConditionIDOut` |

### Medications
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetMedications]` | `@pPatientID`, `@pEncounterID` | `MedicationId`, `Name`, `PrescribedDate` |
| `[HSS].[uspGetPrescribedMedications]` | same | same |
| `[HSS].[uspGetRegularMedications]` | same | same |

### Lab / Radiology
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetLabResults]` | `@pPatientID`, `@pEncounterID`, `@pSinceDate`, `@pUntilDate`, `@pSortOrder` | `ReportId`, `Type`, `Date` |
| `[HSS].[uspGetLaboratoryReportList]` | same | same |
| `[HSS].[uspGetRadiologyReportList]` | same | same |
| `[HSS].[uspGetLaboratoryReportDetails]` | `@pReportID` | `Content` |
| `[HSS].[uspGetRadiologyReportDetails]` | `@pReportID` | `Content` |

### Documents
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetDocuments]` | `@pPatientID`, `@pDirection`, `@pContentType`, `@pReferenceID`, `@pSubject`, `@pSinceDate`, `@pUntilDate`, `@pSortOrder` | `DocumentId`, `PatientId`, `Direction`, `ContentType`, `CreatedAt`, `Subject`, `ReferenceId` |
| `[HSS].[uspGetScannedList]` | same | same |
| `[HSS].[uspGetScannedDetails]` | `@pDocumentID` | above + `Content` (varbinary) |
| `[HSS].[uspFindDocumentByReferenceId]` | `@pReferenceID` | same as list row shape |
| `[HSS].[uspSaveDocument]` | `@pPatientID`, `@pDirection`, `@pContentType`, `@pSubject`, `@pReferenceID`, `@pContent` (varbinary) | output `@pDocumentIDOut` |

### Observations
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetObservations]` | `@pPatientID`, `@pEncounterID`, `@pConceptID` | `ObservationId`, `ConceptId`, `Value`, `RecordedAt` |
| `[HSS].[uspGetPatientMeasurement]` | same | same |
| `[HSS].[uspSaveObservations]` | `@pPatientID`, `@pEncounterID`, `@pHeight`, `@pWeight`, `@pBMI`, `@pBloodPressureSystolic`, `@pBloodPressureDiastolic`, `@pWaistCircumference`, `@pSmokingStatus`, `@pHeartRate`, `@pTemperature` | output `@pObservationIDOut` |

### Encounter Summary Templates
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetTemplateSchema]` | `@pIdentifier` | `Name`, `Caption`, `Type` (one row per field) |
| `[HSS].[uspGetEncounterSummary]` | `@pPatientID`, `@pEncounterID`, `@pIdentifier` | `Fields` (nvarchar/JSON) |
| `[HSS].[uspSaveSummary]` | `@pPatientID`, `@pEncounterID`, `@pIdentifier`, `@pFields` | write-only |

### Recalls
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetRecallCategories]` | `@pGroup` | `CategoryId`, `Name` |
| `[HSS].[uspGetRecalls]` | `@pPatientID` | `RecallId`, `CategoryId`, `DueDate` |
| `[HSS].[uspSaveRecall]` | `@pPatientID`, `@pCategoryID`, `@pDueDate` | output `@pRecallIDOut` |

### Screening
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetScreeningCodes]` | none | `Code`, `Description` |
| `[HSS].[uspSaveScreeningCode]` | `@pPatientID`, `@pEncounterID`, `@pCode`, `@pValue` | write-only (rows-affected checked) |

### Providers
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetProvider]` | `@pPracticeLocationID` | `ProviderId`, `Name`, `PracticeLocationId` |

### Practice / Session Context (COL)
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspGetSurgeryData]` | `@pPracticeID` | any (mapped generically into a dictionary) |
| `[HSS].[uspGetSessionData]` | `@pPracticeID` | any |

### Billing
| Procedure | Params | Result columns |
|---|---|---|
| `[HSS].[uspFindInvoiceByNaturalKey]` | `@pPatientID`, `@pServiceCode`, `@pServiceDate` | `InvoiceId`, `ServiceCode`, `ServiceName`, `AmountInclGST`, `Payee`, `ServiceProvider`, `ServiceDate`, `PegasusReference`, `ClaimShortCode` |
| `[HSS].[uspSaveInvoice]` | `@pPatientID`, `@pServiceCode`, `@pServiceName`, `@pAmountInclGST`, `@pPayee`, `@pServiceProvider`, `@pServiceDate`, `@pPegasusReference`, `@pClaimShortCode` | output `@pInvoiceIDOut` |

---

## Dormant (`DMSDA.cs` port) — not wired to any live endpoint, lowest priority

| Procedure | Params | Notes |
|---|---|---|
| `[dbo].[uspHL7SaveInbox]` | `@pNHINumber`, `@pReceivingFacility`, `@pNZMC`, + ~15 optional fields | output `@pOutputParam` (bigint) |
| `[Task].[uspTaskPathLabInsertUpdate]` | `@pNhiNumber`, `@pEDIAccount`, `@pNZMCNo`, `@pTaskSubject`, `@pInboxFolderItemID` | output `@pOutputParam` (int) |
| `[Profile].[uspGetOrganizationByEDI]` | `@pEDIAccount` | returns `OrgName` |
| `[dbo].[uspDocumentSave]` | `@pDocumentID`, `@pClientID`, `@pCategoryID`, `@pDocumentName`, `@pDocumentTypeID`, `@pDescription`, `@pDocumentKey`, `@pDocumentSize`, `@pProfileID`, `@pDocumentData` (varbinary) | output `@pDocumentIDOut` |
| `[dbo].[uspGetDMSData]` | `@pPageNo`, `@pPageSize` | paged rows |
| `uspUpdateDocumentDetailData` | `@pDocumentID`, `@pIsUpdateID`, `@pIsCorrupt`, `@pDocumentData` (varbinary) | rows-affected |
| `uspUpdateDocumentDetailDataInBulk` | `@ptblDocDetail` (table-valued) | rows-affected |

The confirmed-fixed SQL injection this module carried (`UpdateInboxFolderDocuments`, ported and
parameterized) is a `private`/dead-code method never invoked from the outside — it doesn't call a
named stored procedure at all, it runs a parameterized inline `UPDATE Prompt.tblInboxFolderItem`
statement directly. If `Prompt.tblInboxFolderItem` is ever built, it needs at minimum
`DMSID` (nvarchar) and `InboxFolderItemID` (int) columns.

---

## HISO session table (Block 1, confirmed columns only)

`[Appointment].[tblHealthLinkSession]` — `ProviderID`, `PatientID`, `AppointmentID`, `PracticeID`
are the only columns confirmed by HISO-BR-01. A session-creation timestamp
(referenced as `CreatedAtUtc` in `HisoSessionRepository`) is required for the new 12-hour expiry
but its real column name/existence is **not confirmed** (`PROJECT_STATUS.md` §3 item 25) — flagged
the same way there.

---

## Next step (not started)

Per the user's direction (2026-07-20): catalog first (this document), decide on a fresh dedicated
database later, do not build against `PMS_NZ_V2` or `ClinicBooking`. When ready, this document is
the source for generating the actual `CREATE TABLE`/`CREATE PROCEDURE` script plus dummy seed
data.
