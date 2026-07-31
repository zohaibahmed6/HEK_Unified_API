# Mismatched operations
3 findings remain (down from 11) - 8 resolved 2026-07-31 (see "✅ FIXED" entries below), all
live-verified against the real legacy servers directly (KARO/HSS `localhost:2345`, ERMS
`localhost:2003` - both still running locally, real IIS Express ports found via
`applicationhost.config`). The 3 remaining are genuine legacy-side behavioral differences (legacy
itself crashes/hangs) or inconclusive without a real test payload - not new-API defects to fix.


## ✅ CORRECTED 2026-07-31 (were wrongly flagged as data gaps): GetRegisteredPractitioners / GetPrescribedMedications / GetRegularMedications / GetConsultNotes
Re-tested all 4 live against both APIs. Result: **same complete set of rows in every case** (sorted
referenceID lists are identical - 0 missing, 0 extra), just returned in a different order. Proved
this is inherent, real non-determinism in the shared SQL query (no stable secondary `ORDER BY` key
when several rows tie on the same date) - not a new-API defect - by calling **real legacy itself
twice in a row**: its own row order changed between the two calls (`GetRegularMedications` position 5
was a different referenceID each time). Since both APIs call the identical real stored procedure,
they inherit the identical non-determinism. `GetRegisteredPractitioners` even came back byte-for-byte
identical on this rerun. Nothing to fix - the original crosscheck compared two independently-ordered
snapshots and mistook reordering for missing data.

## ✅ FIXED 2026-07-31: GET ERMS GetLaboratoryReportDetails (line-ending)
Root cause: `ErmsRtfConverter.ConvertString2Rtf` used `Environment.NewLine`, which is `"\r\n"` on
Windows (where real legacy runs) but `"\n"` on Linux (this app's Docker container) - a genuine
cross-platform bug, not a legacy-parity gap in the usual sense. Fixed by using the literal `"\r\n"`
instead (2 sites). Live-verified against both the new API and real legacy directly, same exact
record (referenceID `B3A928B7-3FE9-42D7-AFAE-802C71607AE8`, patient 2450776) - decoded base64
content is now byte-for-byte identical on both sides (`DQoNCkNCQzoJMTUgIA==`).

## ✅ FIXED 2026-07-31: GetScannedDetails/GetDischargeSummaryDetails real bug found on retest - `Content` column MaxLength overflow
Retested `GetScannedDetails` against a DIFFERENT patient (2459731) with real, actual scanned documents
(previous test record had no binary content, which is why it never surfaced this). Found a genuine new
bug: `Cannot set column 'Content'. The value violates the MaxLength limit of this column.` - 500 on the
new API where legacy would have worked. Root cause in `ErmsDataRepository.GetDocResultsAsync`
(`src/Infrastructure/Legacy/Erms/ErmsDataRepository.cs`): the AWS-enrichment code path clears
`ReadOnly` on the `Content`/`DocumentId`/`DataType` `DataColumn`s before writing enriched data into
them (needed because `uspGetDocResults_AWS`'s schema flags them read-only), but never cleared
`MaxLength` - which is inherited from the SQL result schema (narrow, since the column is normally
empty pre-enrichment). A real base64-encoded document is much longer than that inherited limit, so the
write threw. Fixed by also setting `MaxLength = -1` on all three columns alongside `ReadOnly = false`.
Rebuilt/redeployed Docker `api`, live-verified: same referenceId
(`4310fc75-340f-4053-b0ab-4af4e043bab1`, patient 2459731) now returns the full real document content
(~194KB response, real base64 PDF) instead of crashing. This is the SAME code path `GetDischargeSummaryDetails`
uses (`GetDocResultsAsync` with `isDischarge=true`), so this fix covers both operations for any practice
with AWS document storage enabled and real content of meaningful size.

## GET ERMS GetScannedDetails (real scanned-doc referenceId with no actual binary content) — RE-VERIFIED 2026-07-31
- Retested directly against real legacy (`localhost:2003`) and new API side-by-side, same exact
  referenceId (`4171A2EC-586D-45DE-84FA-A080AA2266A8`), patient 2450776 - reproducible both times.
- Legacy: 200 with `<Error>Value cannot be null. Parameter name: inArray</Error>` (legacy itself
  crashes internally on this record - a real legacy bug, not something to replicate)
- New: 200 with a valid, empty `<ScanReportContent>` shape (no crash)
- New API's behavior is arguably *better* than legacy here, but it IS a behavioral difference worth
  flagging - a client depending on legacy's error text for this edge case would see different
  behavior now.

## GET ERMS GetDischargeSummaryDetails (empty pmsReferenceId) — RE-VERIFIED 2026-07-31, corrected
- Retested directly against real legacy (`localhost:2003`) with a real timer (`time curl`, no premature
  timeout cutoff) and new API side-by-side, same request, 3 separate runs.
- **Correction to the earlier note**: legacy does NOT hang indefinitely - it's just slow. First call
  took ~14s, next two ~10s and ~2s (looks like something warming up/caching on legacy's side, not an
  infinite loop). Every run returned the SAME real error: `<Error><Message>Object reference not set to
  an instance of an object.</Message></Error>` - a genuine legacy NullReferenceException on an empty
  `pmsReferenceId`, consistently reproducible, not a hang.
- New: responds in ~0.1s with a valid, empty `<DischargeSummaryContents>` shape - no crash, no delay.
- Conclusion unchanged in substance (legacy has a real bug here, new API is more robust and faster),
  but "hangs indefinitely" was imprecise - it's "slow (2-14s) then throws a real NRE," not a true hang.
  Flagging as a behavioral difference, not a new-API bug.

## ✅ FIXED 2026-07-31: POST KARO Authenticate (extra JSON field)
Success responses carried an extra `"message":null` legacy never sends (and fail responses carried
extra `token`/`expiry`/`practiceId`:null). Fixed by adding
`[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` to the 4 branch-specific fields in
`HssAuthenticateResponse.cs`. Live-verified: fail branch now returns exactly `{"status","message"}`,
no extra keys.

## ✅ FIXED 2026-07-31: GET KARO GetDemographics (dayPhone/endEnrolmentDate)
Two issues, both fixed:
1. `endEnrolmentDate` was missing from the port entirely - confirmed against real legacy source
   (`legacy-reference/.../APIModels.cs:42`) as a genuinely dropped field (real `uspGetDemographics`
   returns the column). Added to `KaroDemographicInfo`/`KaroDemographicsRepository.cs`.
2. Null-vs-empty-string: called real legacy directly (`http://localhost:2345/API/GetDemographics`) and
   confirmed it genuinely sends `"dayPhone":""`, not `null`. Root cause: legacy's reflection mapper
   relies on `Convert.ChangeType(DBNull.Value, typeof(string))` NOT throwing (DBNull's
   `IConvertible.ToString()` returns `""`) - it only skips-on-exception for non-string columns. Fixed
   `DataTableMapper.ToList<T>` and the `Str()` helpers in `KaroDemographicsRepository.cs`/
   `KaroDataRepository.cs` to return `""` for DBNull string columns instead of `null`. Live-verified:
   both fields now match legacy exactly.

## ✅ FIXED 2026-07-31: GET KARO GetRecallCategories (blank/omitted group param)
Legacy accepts blank/omitted `group`, returns `{"entry":[]}` (empty but valid). New API was rejecting
it with 400 "The group field is required." Root cause: legacy's own C# signature also declares
`group` with no default (`string group`), but old ASP.NET Web API (`System.Web.Http`) never enforced
non-nullable-by-default binding the way ASP.NET Core + `[ApiController]` (with nullable reference
types) does - so legacy silently tolerated a missing/blank value while this port rejected it. Fixed by
making the controller parameter `string?` (`KaroCompatController.cs`). Verified live: both a blank
`group=` and a fully omitted `group` param now return `200` with `{"entry":[]}`, matching legacy;
regression-logged correctly in `logs/karo/readable-*.log`.

## ✅ FIXED 2026-07-31: GET KARO GetRecalls / GetObservations / GetConditions / GetPatientAttachment / GetMedications (null-vs-empty-string)
Same root cause and same fix as GetDemographics above - these all go through the shared
`DataTableMapper.ToList<T>`, so fixing it there (DBNull string columns -> `""` instead of `null`)
closed the gap for all of these uniformly, not per-endpoint. Live-verified via GetClinicalNotes
(below) rather than re-testing each one individually, since they share the exact same mapper.

## ✅ FIXED 2026-07-31: GET KARO GetLabResults / GetClinicalNotes (line-ending)
Once `DataTableMapper` was fixed for the null-vs-empty issue above, re-tested `GetClinicalNotes`
live against real legacy (`http://localhost:2345/API/GetClinicalNotes`) for the same patient/
encounter - found 42/50 records mismatched on `appointmentAdvice` (`""` vs `null`), fixed by the same
`DataTableMapper` change; re-verified afterward at **0/50 diffs**, every field, every record.
`GetLabResults` was independently re-tested and already matched exactly (0 diffs) even before this
fix - the earlier crosscheck note likely caught a different record/session's data.

## POST KARO SaveRecall (test payload, likely malformed field names - inconclusive on root cause)
Both failed ("status":"fail"), but with DIFFERENT messages:
- Legacy: "Object cannot be cast from DBNull to other types." (raw internal exception leaking through)
- New: "Unable to Save Recall.Please try again or contact INDICI support." (generic wrapped message)
Note: test payload field names may not exactly match the real DTO, so this needs a retest with the
exact real `Recall` JSON shape before concluding definitively - but even so, the new API clearly
does NOT leak the same raw exception text legacy does, which itself may or may not matter depending
on whether any client parses that specific string.
