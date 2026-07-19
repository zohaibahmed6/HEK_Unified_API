# HISO — Documentation Gap Analysis

**Summary:** HISO ships with **no external documentation** (no Word/SRS spec was provided,
unlike KARO/ERMS) and almost no XML doc comments on its public surface, so this report
captures undocumented/surprising behavior directly from the code to seed the future unified
SRS, plus a few places where code comments actively contradict the surrounding logic.

## Findings

### No external documentation exists
Unlike KARO/ERMS, no Word-document specification or README was supplied for HISO. This
report is therefore built entirely from source-code evidence; every item below is either (a)
behavior that is real but undocumented anywhere, or (b) a comment/code mismatch discovered
during this review.

### 1. Framework version mismatch between compile target and runtime target
`Web.config` sets `<compilation debug="true" targetFramework="4.8"/>` but
`<httpRuntime targetFramework="4.6"/>` two lines below (`Web.config` lines 17-18), while
`Hiso.csproj` targets `v4.8`. This inconsistency is undocumented and its practical effect
(whether ASP.NET quirks-mode behaviors for 4.6 apply) is not explained anywhere.
**Should be documented:** which `httpRuntime` compatibility level is actually intended.

### 2. "Static mode" is a documented stub, not implemented
`getData`'s non-dynamic code path is literally empty:
```csharp
else
{
    Logger.Logging.Instance.WriteEventLog("Static mode enabled.");
    #region Static
    // Add logs same way as above
    #endregion
}
```
(`FormSessionService.svc.cs` lines 271-277.) The comment `"Add logs same way as above"` is a
developer TODO left in production code — there is no external documentation describing
whether "static mode" is a legacy/deprecated mode, a future feature, or currently required by
any live client. **Must be clarified with the business before migration** — silently dropping
"static mode" support could break a client that still depends on `IsDynamic=0`.

### 3. Unreachable code in `saveProcessAction` — dead business logic or an incomplete refactor?
```csharp
objPractitioner.Save(dtPractitioner, objSessionKey);
return true;
objFormMeta.formInstanceId = new FormMetaDataFormInstanceId();
... // ~50 more lines of code that can never execute
```
(`FormSessionService.svc.cs` lines 535-583.) The `return true;` on line 535 makes all
subsequent code in that `if (actionType == "save")` block permanently unreachable, including
a second `Acc45Builder`/`Acc45DiagnosisBuilder`/`Mapper.SaveAccidentInformation` call path.
This is either (a) intentional dead code left after a refactor where ACC45 data started being
saved earlier in `saveDataContainer` instead, or (b) a bug that silently disabled a save path.
**Must be clarified with the business**; do not assume either interpretation when migrating —
this is exactly the kind of "surprising undocumented behavior" the ground rules flag for
explicit capture.

### 4. Business rules live in `Web.config`, not in any design document
Table column lists (`UDT_tblACC45Definition`, `UDT_tblPatient`, etc.), qualifier code lists
(`QualifierList`), DMS document type IDs (`DMSHTMLTypeId`, `DMSPDFTypeId`), and task
status/priority IDs (`TaskPriorityId`, `TaskStatusActive`, `TaskStatusCompleted`,
`ACC45TaskTypeTypeId`) are all defined only as `Web.config` `<appSettings>` key/value pairs
(`Web.config` lines 79-112), with no comment explaining why each column is included/excluded
or what happens if a value is missing/misconfigured (`Utitlity.GetColumnNameByTableName`
throws a `NullReferenceException` if the key is absent, with no friendly error).
**Should be documented:** the intended business meaning of each UDT column set and each
numeric ID/config key, as authoritative input for the SRS's data-dictionary/reference-data
sections.

### 5. `HealthLinkSession.GetByGUID` silently swallows all errors
```csharp
catch (Exception)
{
}
finally
{
    con.Close();
}
return objHLSession; // null if any exception occurred, indistinguishable from "not found"
```
(`Mapper.cs` lines 1036-1046.) This is surprising: a database connectivity failure and an
invalid/unknown session GUID produce the exact same externally-visible result ("Invalid
Session Key" fault). No comment explains this design choice, and it has real operational
consequences (masks outages as auth failures). **Should be documented and likely fixed** —
distinguish "not found" from "error" explicitly.

### 6. Comment/behavior mismatch: `SaveFile` hardcodes a developer's local path
```csharp
public static void SaveFile(XmlDocument xdoc, string fileName, byte type)
{
    string path = "D:\\Projects\\HISO-ServiceProject\\Hiso\\Hiso\\data\\SubmittedData\\";
    ...
}
```
(`Mapper.cs` lines 551-567.) This method writes debug XML dumps to a hardcoded absolute path
that only exists on the original developer's machine; it is called from commented-out debug
lines elsewhere (e.g., `Mapper.cs` `FillXml` line 571: `//SaveFile(xDoc, fileName, 1);` and
`FormSessionService.svc.cs` line 572, also commented out). This confirms the method is a
leftover debugging aid, not documented as such, and would throw an unhandled
`DirectoryNotFoundException` if ever re-enabled in a different environment. Similarly,
`Web.config` appSetting `htmlPath` (line 87) hardcodes
`D:\Projects\HISO-ServiceProject\Hiso\Hiso\data\SubmittedData\` as a production config value.

### 7. `processAction`'s `addInvoice` and `launchForm` actions are silent no-ops
```csharp
else if (request.actionId == "addInvoice")
{
    //Add invoice information into PMS
}
else if (request.actionId == "launchForm")
{
    //Launch a form
}
```
(`FormSessionService.svc.cs` lines 406-413.) These branches do nothing but are accepted
without error; `objProAResp.@return.processed` remains `false` (its initial value) but the
call still returns a `200`-equivalent SOAP success with no fault. A caller invoking
`addInvoice` might reasonably (and incorrectly) assume something happened based on getting a
non-fault response. **Should be documented** as either planned-but-unbuilt or explicitly
unsupported.

### 8. XML doc-comment coverage
No `<summary>` XML doc comments exist on any public class or method in the hand-written
source files (`Mapper.cs`, `Task.cs`, the Builder classes, `DAL/*.cs`) except a single
class-level comment on `FormSessionService` ("Author Azam Khan / Hiso Form Implementation for
PMS" — `FormSessionService.svc.cs` lines 24-27) and a short summary on `DbAccess.selectStoredProcedure`.
Public method signatures throughout give no indication of parameter meaning, side effects, or
failure modes beyond what can be inferred from reading the implementation.

## Risks
- Items 2, 3, and 7 above are exactly the kind of behavior that a rewrite could silently drop
  or silently "fix" in a way that breaks an existing (even if rarely used) integration path —
  each requires explicit business confirmation, not developer judgment, before the unified
  SRS finalizes requirements.
- Item 5 (swallowed exceptions in session resolution) could mask real production incidents as
  routine "invalid session" errors, hiding operational problems from the team.

## Recommendations
- Treat this document as a punch-list of open questions for the client (Zohaib) to resolve
  before/during SRS drafting: confirm status of static mode, the dead ACC45 save branch,
  addInvoice/launchForm, and the config-driven column lists.
- Add XML doc comments to any HISO logic that is retained/ported into the unified platform,
  capturing the WHY documented in `BusinessRules.md` alongside the code.
