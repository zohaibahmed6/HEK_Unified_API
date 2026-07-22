# Unified / Canonical Data Model (best-effort draft)

**Status: NOT signed off.** Per `hek_analysis/PROJECT_STATUS.md`: "Zohaib decided: keep HISO's,
KARO's, and ERMS's response shapes separate for now rather than have the contract force them into
one merged/canonical field set, because he doesn't currently have sample live responses or the
time to do the field-by-field comparison. To be revisited later" (also restated later in the file:
"Zohaib decided not to merge HISO/KARO/ERMS's demographics ... endpoints into one canonical shape
— no sample responses or time available for the field-by-field comparison right now, deferred").

This document is a derived comparison of the four dataset files in this directory, built for this
documentation pass only. It is not a substitute for that deferred reconciliation exercise, and
should not be treated as an approved contract.

## What actually exists as "canonical" today

`src/Contracts/Demographics/DemographicsCanonical.cs` is the one real unified shape in the
codebase (used by `CanonicalDemographicsController`), explicitly designed to null out
fields a source system doesn't produce rather than force a lossy merge:

```
DemographicsCanonical(
    PatientId, PracticeId, FirstName, LastName, DateOfBirth,
    DateOfEnrolment, EndEnrolmentDate, EncounterId, Nhi, Source: OriginScope)
```

## Field intersection across the four demographics DTOs

| Canonical field | Hiso | Karo | Erms | Col |
|---|---|---|---|---|
| PatientId | yes | yes | yes | yes |
| PracticeId | yes | yes | no (null) | yes |
| FirstName | yes | yes | yes | yes |
| LastName | yes | yes | yes | yes |
| DateOfBirth | yes | yes | yes (`Dob`) | no (null) |
| DateOfEnrolment | no | yes | no | no |
| EndEnrolmentDate | no | yes | no | no |
| EncounterId | no | no | yes | no |
| Nhi | no | no | yes | no |

**True intersection (present in all four): PatientId, FirstName, LastName.**
PracticeId is present in 3/4 (missing from Erms). DateOfBirth is present in 3/4 (missing from Col).

## Per-system-only fields
- **Karo-only:** DateOfEnrolment, EndEnrolmentDate.
- **Erms-only:** EncounterId (in this mapping), Nhi.
- **Col:** contributes no fields beyond the intersection in the canonical mapping today, but its
  underlying `ColPatientData` DTO (see `erms.md`) carries a much larger real field set (NHI, GMS/CSC/HUC
  card status, ethnicity codes ×6, next-of-kin, enrolment/capitation flags) that has not been mapped
  into the canonical model at all.

## Confirmed vs. inferred, per system (see individual dataset docs for detail)
- **Hiso:** demographics mapping confirmed live (concept-driven, real procedure resolution).
- **Karo:** demographics mapping confirmed live against real `PMS_NZ_V2` data.
- **Erms:** demographics mapping confirmed live against real data (same procedure as Karo).
- **Col:** demographics mapping shape only — underlying procedure and column semantics are an
  unconfirmed inference (not exercised against live data as of this writing).

## Caveat
Because full field-by-field reconciliation was explicitly deferred by the stakeholder, this
intersection/union table should be treated as a snapshot of what the code currently does, not a
recommendation for what the canonical model should contain. Any decision to expand
`DemographicsCanonical` (e.g. to include Col's richer patient-registration fields) needs the
deferred comparison exercise first.
