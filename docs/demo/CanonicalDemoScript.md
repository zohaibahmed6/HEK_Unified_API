# Canonical Demographics Demo Script

Demonstrates HEK_UNIFIED_API_SPEC.md's acceptance criteria (§7): one unified endpoint, field
selection, out-of-scope fields blocked, an audit trail, and a trace ID — without touching any of
the existing legacy-compat endpoints (`/hiso/*`, `/karo/*`, `/erms/*`, `/erms/col/*`), which remain
live and unchanged.

## Endpoint

```
GET /v1/patients/{patientId}/demographics?fields=firstName,lastName,dateOfBirth
Authorization: Bearer <token>
```

The token's `originScope` claim (Hiso/Karo/Erms/Col) determines which legacy system's data is
fetched and which fields the caller is allowed to see — the route itself never changes.

## 1. Simulate the HISO caller

```bash
curl -s "https://localhost:5299/v1/patients/2459731/demographics?fields=firstName,lastName,dateOfBirth" \
  -H "Authorization: Bearer $HISO_TOKEN"
```
Expected: `200`, canonical JSON with `patientId`, `practiceId`, `firstName`, `lastName`,
`dateOfBirth` — same real data HISO's own `getData` flow already proved. Console shows a
`CanonicalDemographicsAccess consumer=Hiso ... fieldsReturned=...` log line and an
`X-Correlation-ID` response header.

## 2. Simulate the KARO caller, requesting a KARO-only field

```bash
curl -s "https://localhost:5299/v1/patients/2459731/demographics?fields=firstName,dateOfEnrolment" \
  -H "Authorization: Bearer $KARO_TOKEN"
```
Expected: `200`, only `firstName` and `dateOfEnrolment` in the body — proves sparse fieldsets work
(FR-4).

## 3. Simulate the ERMS caller asking for a field outside its scope

```bash
curl -s "https://localhost:5299/v1/patients/2459731/demographics?fields=firstName,dateOfEnrolment" \
  -H "Authorization: Bearer $ERMS_TOKEN"
```
Expected: `200`, body contains `firstName` only — `dateOfEnrolment` is a KARO-only field, silently
dropped for an ERMS-scoped token (FR-5: "no extra data goes to anyone"), not an error.

## 4. Cross-patient block (unchanged existing behaviour, still enforced on the new route)

```bash
curl -s "https://localhost:5299/v1/patients/999999/demographics" -H "Authorization: Bearer $HISO_TOKEN"
```
Expected: `403` — `EnsurePatientScope` rejects a token not scoped to patient 999999 (ADR-003),
proving the canonical layer inherits the same authorization guarantees as every other endpoint.

## 5. Audit trail

Every call above produces one structured Serilog line:
```
CanonicalDemographicsAccess consumer=Karo practiceId=TEST-PRACTICE-001 patientId=2459731 endpoint=/v1/patients/2459731/demographics fieldsReturned=firstName,dateOfEnrolment
```
correlated by the same `X-Correlation-ID` returned in the response header — satisfies FR-6 (who
called, when, exactly which fields).

## 6. Confirm legacy endpoints are untouched

```bash
curl -s "https://localhost:5299/karo/authenticate?..."
curl -s -X POST "https://localhost:5299/hiso/getData" ...
```
Both return their exact pre-existing legacy-shaped responses — the canonical layer is additive,
not a replacement.
