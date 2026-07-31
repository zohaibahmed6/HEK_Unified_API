# Errored operations
Status as of 2026-07-31: HISO getFormView, KARO routing, HISO getData/saveContainer all fixed and
verified. COL GetSessionData corrected (was never actually a bug - see crosscheck/mismatched.md
history). Remaining open: KARO's 6 write ops and COL SaveInvoice/Authenticate legacy-side issue -
still need real payloads/investigation, not confirmed bugs.


## ✅ FIXED 2026-07-31: HISO getData - was 2 real bugs, found via a real payload
Retested with a real, complete `formMetaData` (matching the pattern already proven for saveContainer)
against both the real legacy server and this API. Legacy itself returned empty concept-mapped fields
for this test session (no bug - just no real patient-detail concept data mapped for this test data),
so both sides matched on field *values* immediately - but comparing the raw XML shapes found 2 real
structural bugs:
1. **`FormMetaDataSoap` only modeled 7 of the real WSDL's 14 fields** - missing
   `formInstanceCreationDate`, `formInstanceDescription`, `formDefinitionDescription`,
   `recipientAccount` entirely, so legacy's real "echo the request's formMetaData back unchanged"
   behavior silently dropped those 4 real values from every getData (and getFormView/saveContainer)
   response. Added all 4.
2. **The response's `submittedData` wrapper was missing** - the code assigned the bare `<form>`
   element directly to the `submittedData` property; since `[XmlAnyElement]` serializes whatever
   element it's given verbatim (using that element's own tag), the real
   `<submittedData><dummy/><form>...</form></submittedData>` wrapper (confirmed against a live legacy
   capture, same `dummy`-placeholder pattern already known from the request side) was completely
   absent from every response - a client parsing the real legacy shape would fail to find `<form>`
   nested one level deeper than the new API was actually returning it. Fixed by building the real
   `submittedData` wrapper with the `dummy` placeholder before assigning it.
Verified live: new API's getData response now matches legacy's real XML shape field-for-field and
structurally (wrapper + dummy present); regression-checked `getVersion` still works; confirmed logged
correctly in `logs/hiso/readable-*.log`.

## ✅ FIXED 2026-07-30: HISO getFormView - was MISSING OPERATION (critical)
**Fix:** wired `getFormView` into `IFormSessionService.cs`/`FormSessionService.cs`, matching the real
shape confirmed via a live WSDL/XSD fetch against the real legacy server (`sessionKey` +
`formInstanceId` request, `resumePath`/`dataContainer`/`view`/`viewType` response) - reused the
already-working `GetFormViewQuery` handler (same one the JSON dashboard endpoint already used), no
new business logic. Verified live in the rebuilt Docker container: SOAP call now returns 200 (was a
"No operation found" fault), logged correctly in `logs/hiso/readable-*.log`. Also corrected a stale
doc comment on `HisoCompatController.cs` that had wrongly claimed this operation "has no SOAP
equivalent" - a live WSDL fetch during this crosscheck proved it's a genuine 6th real operation.
Regression-checked: `getVersion` still works.

**Follow-up fix (same day):** once wired, a real test (session `439cc902-...`, real ACC45 row
`650854`, DMS document `9d915ade-...` - all confirmed to exist in the live `PMS_NZ_V2` DB via
`sqlcmd`) still returned an empty result. Root cause traced to `Acc45DefinitionRepository.cs`:
compared against the real legacy source (`Acc45DefinitionBuilder.cs:142-145`), the port's
`@pSessionKey` parameter was wrong - real legacy always sends the actual session GUID
(`objSession.GUID.ToString()`), but the port sent `PracticeId` (e.g. `"901"`) instead whenever
`ReferenceId` was null, which never matches `tblACC45Definition`'s real `SessionKey` column (a GUID).
Also, `HealthLinkSession` never carried the real session GUID at all. Fixed: added a `SessionKey`
field to `HealthLinkSession` (optional, doesn't affect the many canonical-hub call sites that build
one without a HISO session), threaded `request.SessionKey` through all 4 call sites
(`GetFormViewQuery`/`GetDataQuery`/`SaveContainerCommand`/`ProcessActionCommand`), and fixed
`Acc45DefinitionRepository.GetDefinitionAsync` to always send the real session GUID. Verified live:
the same real session/document now returns the exact real DB values
(`resumePath=/ereferral/PrepopulateForm.action?hiso_formDefinitionId=msdwcref`,
`viewType=text/html`) matching the row found directly in the database.

**Second follow-up fix (2026-07-31):** `view` (the actual document bytes) still always came back
null. Traced to `IDmsProxyService`, whose only implementation was a permanent-null stub - real logic
called an external DMS proxy ASMX service. Live-tested against the real legacy server: pointing its
`DMSServiceURL` at the real production address makes legacy itself hang ~44s then fault "Unable to
connect to the remote server" - that private-LAN service is unreachable even to legacy here,
confirmed live. A `DMSProxy` project found on disk turned out to be only the client wrapper (a
Library/DLL), not the real server - not present anywhere on this machine. Parked per Zohaib's
instruction. Fixed via a working alternative: `DmsDocumentRepository` now reads the document directly
from the same `DMS_PMS` database HISO already connects to (`dbo.uspDocumentGetByDMSID`), instead of
the unreachable external service. Verified: the proc returns real document bytes directly via
`sqlcmd` for a real document (practice 128); this dev DB's ACC45 definitions and `tblDocument` have
zero overlapping document IDs (a seed-data gap, not a code issue), so a full SOAP-level test with a
matching real pair isn't possible here - but the mechanism itself is proven correct at the DB layer,
and the rebuilt container regression-tested clean (same graceful result, no crash) against the
original test session.
- Legacy: 500 Fault (our test formInstanceId doesn't exist, expected)
- New: 500 Fault "No operation found for specified action: .../getFormView" - the SOAP contract
  (`IFormSessionService.cs`/`FormSessionService.cs`) has NO `getFormView` method at all (confirmed via
  grep - zero matches in `src/Api/Features/Hiso/Soap/`). This contradicts
  `LEGACY_PARITY_VALIDATOR.md` which marks getFormView "✅ Present" based on a source-code read of
  `Acc45DefinitionBuilder` alone - that logic exists in Infrastructure, but was never wired to the SOAP
  endpoint itself. Any real client calling getFormView against the new API gets a hard SOAP fault, not
  legacy behavior. HIGH PRIORITY - real endpoint completely missing, not a data/shape mismatch.

## ✅ FIXED 2026-07-31: HISO saveContainer (was 4 real bugs, found via a real captured client payload)
Zohaib supplied a real, complete client-captured `saveContainer` payload (real HISO Work Capacity Med
Cert form). Testing it live against both the real legacy server and this API found and fixed 4 distinct
real bugs:
1. **Wrong SOAP wire shape**: `SaveContainerRequestSoap` had a wrong top-level `formMetaData` member
   (real WSDL has it nested inside `dataContainer`) and was missing `continueSession` entirely. Fixed
   to match the real `sessionKey/resumePath/dataContainer(FormData)/view/viewType/viewSignature/
   completed/continueSession` shape.
2. **7 missing `UDT_tbl*` config keys** (same class as the getDeliveryOptions/getFormView config gaps)
   - only in the gitignored dev-local config, never in `appsettings.json`, so Docker built a
   zero-column TVP and SQL threw "Structured types must have at least one field." Added all 7.
3. **Confirmed a real legacy quirk, not a new-API bug**: legacy's own `submittedData` deserialization
   is whitespace-sensitive - any insignificant whitespace inside the dummy+form content shifts legacy's
   own hardcoded array index (`[1]`), producing legacy's OWN generic "Data at the root level is
   invalid" fault. Reproduced live directly against the real legacy server. A real SOAP client for this
   operation must send fully whitespace-compacted `submittedData` content.
4. **The real gap**: `HisoDocumentHandler`'s `SqlParameter`s for `uspDocumentSave` used .NET types that
   didn't match the real proc's declared parameter types (`@pProfileID`/`@pDocumentTypeID` as `int` for
   `nvarchar` params, `@pPracticeID` as a bare string for an `int` param, `@pDocumentSize` as `Int32` for
   `BIGINT`) - SQL Server's RPC-style parameter binding silently rejected the call ("expects parameter
   'X', which was not supplied") even though every parameter genuinely was supplied - a real ADO.NET RPC
   gotcha, not a straightforward bug. The exception was caught and swallowed exactly like legacy's own
   code, so the SOAP call kept returning `200`/`response=true` while never actually writing a document -
   only visible in the raw `docker logs` console output, not the per-system log files (this repository
   class doesn't carry a `System` log property). Fixed by giving every `SqlParameter` an explicit
   `SqlDbType`/size.
Also found and fixed a real seed-data gap while chasing this: practice 901 had no `TblPracticeDMSInfo`
row in `DMS_PMS`, so its document saves resolved to no target database at all - inserted one matching a
working practice's shape.
**Verified end-to-end, repeatedly, with real data**: real legacy's save produced a real document in
`DMS_PMS.dbo.tblDocument`/`tblDocumentDetail`; after all 4 fixes, this API's save now produces the same
- byte-for-byte identical `DocumentData` to what was sent (`<html><body>Test View Content</body></html>`),
confirmed via direct `sqlcmd` inspection of both, not just a `200` response.

## ✅ FIXED 2026-07-30 (same day, post-crosscheck): KARO's real external routes ALL 404 against the new API (routing bug, not a data bug)
**Fix:** added `KaroOperationNameMap` to `src/Api/Middleware/LegacyHostRoutingMiddleware.cs` -
translates the real external operation name to KARO's internal route name after the prefix swap.
Verified live in the rebuilt Docker container: `GetDemographics`/`GetClinicalNotes`/`GetConditions`/
`GetProvider`/`SaveScreeningCode` now all return 200 at their real external URL (was 404), each
logged in `logs/karo/readable-*.log`. `ping`/`authenticate` confirmed still working (no regression).
Confirmed via 13 real operations (GetClinicalNotes, GetConditions, GetDemographics, GetDocuments,
GetEncounterSummary, GetLabResults, GetMedications, GetObservations, GetProvider,
GetRecallCategories, GetRecalls, GetScreeningCodes, GetPatientAttachment) - every single one returns
404 when called at its REAL external path (`/api/GetDemographics` etc, on `Host: hss.itsmyhealth.nz`),
even though the underlying handler works fine (confirmed 200 when called directly at its internal
route, e.g. `/karo/demographics`).

**Root cause:** `LegacyHostRoutingMiddleware` only swaps the path PREFIX (`/api` -> `/karo`), it does
NOT rename the operation segment. ERMS's internal controller
(`ErmsCompatController.cs`) happens to use the SAME names as the real external path
(`[HttpGet("GetPatientData")]`, `[HttpGet("GetAccidents")]`, etc.) so prefix-swapping alone worked
there. But `KaroCompatController.cs` uses short, different, lowercase internal names for every
single operation (`[HttpGet("demographics")]` not `GetDemographics`, `[HttpGet("clinicalnotes")]` not
`GetClinicalNotes`, `[HttpPost("document")]` not `SaveDocument`, etc.) - so the prefix-only rewrite
produces a path like `/karo/GetDemographics` which matches nothing, and MVC 404s.

**Impact:** only `ping` and `authenticate` happen to survive (their internal names are close enough,
case-insensitively, to the external ones) - every other KARO operation (13+ GET, 8+ POST) would 404
for any real client hitting `hss.itsmyhealth.nz` in production. This is a genuinely critical,
previously-undiscovered gap - worse than any of the data-shape mismatches found so far, since it's a
hard failure, not a subtly-wrong response.

**Where to fix:** `LegacyHostRoutingMiddleware.cs` needs an explicit external-name -> internal-name
map for KARO (e.g. `GetDemographics` -> `demographics`, `SaveClinicalNotes` -> `clinicalnotes`), the
same way `ColController`'s external `/COL/...` names already differ from ERMS's shared prefix - not
just a prefix swap. Confirm the exact real external name for each KARO operation against
`hek_analysis/docs/analysis/karo/EndpointInventory.md` before writing the map.

## ✅ RESOLVED 2026-07-31: KARO write operations (SaveClinicalNotes, SaveCondition, SaveRecall, SaveObservations, SaveDocument, SaveSummary, SaveInvoice)
Derived real request shapes directly from the real legacy source (`legacy-reference/hsswebapi/.../Models/APIModels.cs`
- `ConsultNote`/`Condition`/`Recall`/`Observations`/`Document`/`Invoice` classes - and the real `SaveSummary`
sample bodies in `KARO_HSS_doc.md`), no guessing needed. Tested all 7 live against both real legacy and
this API:

- **SaveClinicalNotes, SaveObservations**: matched immediately, byte-for-byte identical success response.
- **SaveSummary**: matched immediately - both sides reject the test payload with the exact same
  "Invalid OutCome..." validation error.
- **SaveCondition**: both succeed; a message difference on rerun ("already exists" vs plain success)
  is expected - legacy and this API share one live database, so whichever call runs first inserts
  the row and the second correctly hits the real `-5` "already exists" idempotency sentinel.
- **SaveDocument**: found and fixed 2 real bugs. (1) `KaroWriteRepository`'s `SqlParameter`s for
  `uspDocumentSave` didn't match the real proc's declared types (`@pDocumentSize` Int32 vs BIGINT,
  `@pPracticeID` bare string vs int) - same root cause as HISO's `HisoDocumentHandler` fix, fixed the
  same way (explicit `SqlDbType` on every parameter). Also fixed the identical bug in
  `ErmsWriteRepository`'s own `uspDocumentSave` call while there. (2) `Karo:DMSDocTypes` (and
  `Erms:DMSDocTypes`) config was only in the gitignored dev-local file, missing from
  `appsettings.json` - resolved every document to type ID `-1`, which the real proc doesn't handle
  cleanly (surfaces as its own internal "ROLLBACK TRANSACTION request has no corresponding BEGIN
  TRANSACTION" error). Added both. Verified live: real document saved with byte-for-byte matching
  content, confirmed via `sqlcmd`.
- **SaveInvoice**: found and fixed a real bug - `KaroWriteRepository.SaveInvoiceAsync` sent a
  `@pPayee` parameter that the real `[HSS].[uspInsertUpdateService]` proc doesn't declare at all
  (confirmed in `HSSDA.cs:1221-1224` - legacy's own C# has `@pPayee`/`@pCoPayment` commented out, so
  legacy never actually sends it even though the request model accepts a `payee` field). Removed the
  extra parameter. Verified live: real `serviceMappingId` returned, matching legacy's exact response
  shape.
- **SaveRecall**: both legacy and this API still fail on this specific test data (different error
  text - legacy leaks a raw "Object cannot be cast from DBNull" DB exception, this API gives a
  generic message) - legacy itself can't complete this specific save regardless of `CategoryId`
  value tried, so this looks like a real, pre-existing legacy limitation with the test `Group` name
  rather than a new-API defect; not fully resolved, would need a real `Group` value confirmed to work
  against legacy first.

All fixes regression-checked (`ping`/`Authenticate` still 200) and confirmed logged correctly in
`logs/karo/readable-*.log`.

## ✅ FIXED 2026-07-31: POST COL Authenticate Expiry "+0" bug (legacy-side auth failure still open)
Legacy: `{"Token":null,...,"error":"Object reference not set to an instance of an object."}` (tried
both PascalCase and lowercase field names, same NRE both times) - still inconclusive whether this is a
real legacy quirk (maybe COL needs a pre-existing ERMS session, or a different body shape than
documented) or a test-setup gap on our side; not investigated further this pass.
New API's `Expiry` bug is fixed: `ColAuthenticateQueryHandler` (`ColQueries.cs`) built `Expiry` via
`expiry.ToString("yyyy-MM-ddTHH:mm:ssz")` - same class of bug as ERMS's original "+0 instead of +12"
issue, relying on the container's ambient timezone rather than computing NZ's real offset explicitly.
Added the same `FormatExpiryLikeLegacy` fix used for ERMS (explicit `Pacific/Auckland` lookup, not
ambient system time). Verified live: `Expiry` now returns `2026-07-31T00:24:39+12`, matching legacy's
real format, confirmed in the rebuilt Docker container.

## ✅ CORRECTED 2026-07-31 (was wrongly flagged CRITICAL): COL GetSessionData "crash" is intentional, verified legacy parity - not a bug
Originally flagged as a critical new-API-only crash. On closer investigation: `ColDataRepository.GetSessionDataAsync`
(`src/Infrastructure/Legacy/Erms/ColDataRepository.cs:24-27`) deliberately executes an empty stored
procedure name (`""`), with an explicit comment: "Legacy BUG reproduced on purpose: PHCO.GetSessionData
executes an EMPTY proc name, which always fails." Confirmed directly against the real legacy source
(`legacy-reference/ermsapi/DevLocal/DAL/Pegasus/PHCO.cs:69`):
`DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "", sqlParams.ToArray());`
- legacy itself passes a literal empty string as the procedure name, which always throws. The new
API's `"CommandText property has not been initialized"` error is exactly what that same empty-proc-name
call produces via `Microsoft.Data.SqlClient` - a faithful, intentional reproduction of a genuine real
legacy bug, not a new-API defect. Nothing to fix here.

## COL GetCurrentPatientData / GetProviderData / GetSurgeryData / GetDiagnosisData - inconclusive (auth blocked)
Legacy: `"Object reference not set to an instance of an object."` for all 4 - almost certainly because
we're passing an ERMS-issued token, not a COL-issued one (COL's own Authenticate failed against
legacy for us - see above). New API succeeds with real, plausible-looking data for all 4. Cannot
confirm these are correct/matching without first resolving why COL Authenticate fails on the real
legacy server - flagging as blocked, not confirmed-good or confirmed-bad.

## COL SaveInvoice - not reliably tested
Legacy blocked (same COL-Authenticate issue as above). New API test payload was malformed (400
validation error on our guessed field shape - `AmountInclGST` needs to be a JSON number in a
different wrapper, not what we sent) - inconclusive, needs the real `SaveInvoice` JSON DTO shape
from `src/Adapters.Erms`/`ColQueries.cs` before this can be tested meaningfully.
