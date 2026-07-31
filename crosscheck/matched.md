# Matched operations
Run date: 2026-07-30. Legacy servers: HISO :53507, ERMS/COL :2003, KARO/HSS :2345. New API: :8080 (Docker).
~20 operations confirmed byte-identical (or value-identical modulo wrapper-naming) between legacy and new API.


## POST ERMS Authenticate
Legacy and New: byte-for-byte identical - <Authentication><Token>cba9201a-b1c6-459d-8ec1-1af5e203acd3</Token><Expiry>2026-07-31T00:24:39+12</Expiry><PracticeId>maraenui</PracticeId></Authentication>

## GET ERMS Ping
Legacy and New: identical - <Ping>Success!</Ping>

## GET ERMS (byte-identical, patient 2450776 / encounter 19592581__901__FZZ999-B)
- GetPatientData
- GetPatientMeasurement
- GetSmokingStatus
- GetCurrentUser
- GetNextOfKin
- GetAccidents
- GetClassifications
- GetMedicalAllergies
- GetLaboratoryReportList
- GetRadiologyReportList
- GetDischargeSummaryReportList
- GetScannedList
- GetRadiologyReportDetails (identical, with pmsReferenceId param)

## ERMS SaveDocument (write)
Both legacy and new: 400 BadRequest, identical body "BadRequest" (same rejection with this test payload/shape).

## KARO ping
Both: {"status":"success"}

## KARO GET (data-identical via internal route, real routing bug tracked separately above)
- GetScreeningCodes
- GetEncounterSummary
- GetProvider
- GetDocuments

## POST KARO SaveScreeningCode
Both: {"status":"success","message":""} - identical (confirms real legacy no-op stub behavior reproduced exactly)
