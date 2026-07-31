# Session Handoff — 2026-07-31

Simple zaban mein: is session mein kya hua, kya fix hua, live-verify kya hua, aur kal kahan se
shuru karna hai. Naye terminal/session ko pura context yahin se mil jayega.

## Is session ka scope

Pichle session ka kaam tha: KARO ke baaki 6 write-ops ka real payload nikal ke test karna (khatam ho
chuka tha). Ye session shuru hua ek sawal se: "technical logs sahi likh rahe hain ya kuch miss ho raha
hai" — aur wahan se logging observability + baaki cosmetic fixes tak phaila.

## 1. Logging observability overhaul (✅ complete, live-verified)

**Masla jo mila:** `HisoDocumentHandler` (aur 3 aur HISO Infrastructure-layer files) ke error logs
sirf `docker logs` mein dikhte thay, per-system file (`logs/hiso/*.log`) mein kabhi nahi jaate thay —
kyunke un calls mein `"System"` tag missing tha.

**Fix kiya (3 hisso mein):**

1. **`src/Api/Middleware/RequestResponseLoggingMiddleware.cs`** (naya file) — har request ke liye:
   - Path/Host se `System` tag resolve karta hai (karo/erms/col/hiso/hiso-soap) aur Serilog
     `LogContext` mein push karta hai — is se **koi bhi** log line (chahe kahin bhi likhi ho, chahe
     tag na bhi lagaya ho) automatically sahi per-system file mein chali jati hai.
   - Poora raw request body + response body bhi ek line mein log karta hai (50,000 chars tak, uske
     baad truncate with total-length note).
   - `Program.cs` mein `app.UseCorrelationId()` ke turant baad, SOAP endpoint se pehle register kiya
     hai (taake SOAP traffic bhi cover ho, jo baaki pipeline se pehle hi short-circuit ho jata hai).

2. **`src/Infrastructure/Legacy/LegacyDbExecutor.cs`** — is static helper (jo ~30 repositories use
   karte hain) mein ab **har** SQL/stored-procedure call automatically log hoti hai: SP ka naam, har
   parameter ki value (binary/image columns sirf size ke sath, e.g. `byte[12345]` — pura data JSON
   mein dump karna bekar/bohot bada hota), aur result (rows/columns, ya poori exception agar fail
   ho). `Serilog.Log` (static logger) use kiya hai kyunke ye static class hai, DI nahi hai — is wajah
   se Infrastructure project mein `Serilog` package add karna pada (`Directory.Packages.props` +
   `HekCoreApi.Infrastructure.csproj`).

3. **7 untagged `_logger.LogError/LogWarning` calls fix kiye** (`HisoDocumentHandler.cs`,
   `AsposeMimeConverter.cs`, `HisoConceptExecutor.cs`, `HisoRequestEngine.cs`) — `"{System}"` tag add
   kiya har jagah (belt-and-suspenders, ambient middleware ke sath).

**Live-verify kiya** (sab 4 systems): KARO (`/karo/demographics` → `uspInsertAndValidateToken` +
`uspGetDemographics` dono SP names/params/rows file mein confirm), ERMS (`/erms/ping`), HISO-SOAP
(`getVersion` — is call ke dauran ek real `NullReferenceException` bhi pakड़ा gaya, dekho niche),
COL (`/erms/col/authenticate`).

**Har call ko poori tarah trace karne ka tareeqa:** ek hi `CorrelationId` se `logs/{system}/technical-*.log`
mein grep karo — 4 lines milengi: (1) `"SQL call: {Procedure}..."` (SP + params + result), (2)
`"...verbose payload..."` (controller-level context), (3) `"...RequestBody/ResponseBody..."` (raw
HTTP), (4) `"[CallLog]..."` (readable summary).

## 2. HISO getVersion "crash" — false alarm, no fix needed

Ek test call se `getVersion` crash hua (NullReferenceException) — lekin root cause nikla: maine test
mein GALAT SOAP namespace use kiya tha (`http://healthlink.net/FormSessionService` ki jagah asli
`http://www.hiso.govt.nz/10014.2/1.0/formsession`). Sahi namespace se retest kiya to bilkul sahi
response mila. **Koi code change nahi kiya** — real bug nahi tha.

## 3. HISO `getDeliveryOptions` config (✅ complete, live-verified)

**Masla:** `GetDeliveryOptionsQuery.cs` ke 4 config keys (`Hiso:PracticeEdi`/`UserId`/`Password`/`Url`)
sirf gitignored `appsettings.Development.local.json` mein thay — Docker/Azure builds mein bilkul
missing thay, isliye senderAccount/senderPassword/url hamesha khali aata tha.

**Fix:** `src/Api/appsettings.json` mein 4 keys add ki, `CHANGE_ME_HISO_DELIVERY_*` placeholders ke
sath (UserId/Password/Url ke liye — kyunke real credential kahin bhi, local dev mein bhi, nahi tha).
`PracticeEdi` ka real value ("1") directly likha (ye secret nahi, boolean flag hai).

**Self-caught bug:** pehle `docker-compose.yml` mein `Hiso__UserId: ${HISO_DELIVERY_USERID}` jaisi
unconditional env-var mappings add ki thin — live test se pakड़ा ke unset host env var bhi container
ko khali string deta hai, jo appsettings.json ke CHANGE_ME ko silently overwrite kar deta (ASP.NET
Core env vars appsettings se zyada priority rakhte hain, chahe khali hi kyun na hon). Wo 3 lines hata
di — ab tak sirf appsettings.json ka placeholder use hota hai. **Jab real HealthLink credentials
milein**, `docker-compose.yml` mein comment ke pass wapis 3 env-var lines add karni hain
(`Hiso__UserId`/`Hiso__Password`/`Hiso__Url`, `HISO_DMS_*` jaisa pattern already wahan hai) aur `.env`
mein real values dalni hain.

**Live-verify:** `senderAccount` session ki real PracticeEDI se aata hai (`Testn28n6ujh`), baaki 2 field
CHANGE_ME placeholder dikhate hain — silent khali hone se behtar signal.

## 4. Cosmetic mismatches (3 fix ho gaye, 2 abhi baaki — LIVE TEST KARNA HAI)

### ✅ Fix ho gaye, live-verified:

1. **HISO SOAP wrapper naming** (`getVersion`/`processAction`/`getDeliveryOptions`) — real legacy
   `<return>` bhejta hai, hum `<XxxResult><XxxResponseReturn>` bhej rahe thay (kyunke in 3 response
   classes mein explicit `[MessageContract]` nahi tha). Fix: `[MessageContract]` + `[XmlElement("return")]`
   add kiya `src/Api/Features/Hiso/Soap/IFormSessionService.cs` mein. **Note:** `[MessageBodyMember(Name=...)]`
   is mode mein (SoapCore `SoapSerializer.XmlSerializer`) kaam NAHI karta — `[XmlElement]` hi asli
   attribute hai jo respect hota hai (live test se hi pata chala, pehli koshish fail hui thi).

2. **KARO Authenticate extra field** — success response mein `"message":null` extra aata tha (aur fail
   mein `token/expiry/practiceId:null` extra). Fix: `HssAuthenticateResponse.cs` mein
   `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` add kiya 4 fields par.

3. **KARO GetDemographics missing `endEnrolmentDate`** — real legacy source (`APIModels.cs:42`) se
   confirm kiya, genuinely dropped field tha. Fix: `KaroDemographicInfo` record +
   `KaroDemographicsRepository.cs` mein add kiya.

### ✅ Dono resolve ho gaye (usi din, thodi der baad) — ab koi pending cosmetic item nahi bacha

Real legacy servers **abhi bhi locally chal rahe hain** — inke IIS Express ports
`applicationhost.config` se nikale: **KARO/HSS → `http://localhost:2345`**, **ERMS →
`http://localhost:2003`** (dono confirm `netstat`/live curl se). Route pattern legacy ka
`{controller}/{action}/{id}` hai (koi `api/` prefix nahi, controller name literally `API`/`api` hai) —
e.g. `http://localhost:2345/API/GetDemographics?system=hss&pho=901&patientId=...&encounterId=...`
(system/pho/patientId/encounterId sab 4 query params zaroori hain, warna 404 aata hai "no action
matches").

**A. Null-vs-empty-string** — real legacy ko directly call kiya, confirm hua ke wo sach mein
`"dayPhone":""`/`"endEnrolmentDate":""` bhejta hai (mera pehla code-analysis galat tha). Asal wajah:
`Convert.ChangeType(DBNull.Value, typeof(string))` throw NAHI karta (DBNull ka `ToString()` empty
string deta hai) — sirf value-types (int/DateTime) ke liye throw hota hai. Fix:
`DataTableMapper.ToList<T>` + `KaroDemographicsRepository.Str()`/`KaroDataRepository.Str()` ab DBNull
string columns ke liye `""` return karte hain, `null` nahi. Live-verify: `GetDemographics` aur
`GetClinicalNotes` (jahan 42/50 records mismatch thay) dono ab 0 diffs.

**B. Line-ending farq** — root cause `ErmsRtfConverter.ConvertString2Rtf` mein `Environment.NewLine`
tha, jo Windows (legacy) par `\r\n` aur Linux Docker (hamara container) par `\n` resolve hota hai —
genuine cross-platform bug. Fix: literal `"\r\n"` use kiya. Live-verify: `GetLaboratoryReportDetails`
ka base64 content ab byte-for-byte legacy jaisa (`DQoNCkNCQzoJMTUgIA==` dono taraf).

Sab kuch build + Docker rebuild/redeploy + live-test ho chuka hai is session mein.

## Zaroori files/paths jo yaad rakhna hai

- Config: `src/Api/appsettings.json` (production), `src/Api/appsettings.Development.local.json`
  (gitignored, real dev secrets/values yahan hain — reference ke liye padho, kabhi commit mat karo)
- Docker: `docker-compose.yml` (env var mappings), rebuild command: `docker compose build api && docker compose up -d api`
- Logs: `logs/{karo,erms,hiso,col}/technical-*.log` (JSON), `readable-*.log` (plain), `errors-*.log`
  (Warning+) — sab `CorrelationId` se linked
- Crosscheck docs: `crosscheck/SUMMARY.md`, `crosscheck/errors.md`, `crosscheck/mismatched.md`,
  `crosscheck/dashboard.html` — inhe bhi update karna hai jab (A) aur (B) resolve ho jayen
- Master doc: `docs/PROJECT_MASTER.md` §2 "Current state" — is session ke sab fixes wahan dated
  entries ke tor par likhe hain, wahi se pura context mil sakta hai agar ye file kabhi stale ho jaye

## Live-test karne ka tareeqa (reference)

Real legacy KARO ka base URL/host aur test credentials is session mein pehle use ho chuke hain
(patientId 2450776, encounterId 19592581__901__FZZ999-B, practice 901/FZZ999-B) — dhoondne ke liye
`docs/PROJECT_MASTER.md` aur `crosscheck/PARITY_MEMORY.md` mein raw request/response capture maujood
hain.

## 5. COL Authenticate + 4 GET operations — ✅ confirmed (real production credentials se test kiya)

Zohaib ne real production COL credentials diye (kahin bhi file mein save nahi kiye, sirf live curl
mein use hue). Result:

- **Naya API**: Authenticate 0.68s mein real token deta hai; `GetCurrentPatientData`/
  `GetProviderData`/`GetSurgeryData`/`GetDiagnosisData` sab real data ke sath 200 OK dete hain.
- **Real legacy**: Authenticate **4+ minute hang** ho gaya (koi response nahi). Root cause: is
  account/practice ki connection-string (`Web.config`, ERMS/COL ka shared IIS project) ek remote IP
  (`43.255.162.58`) par point karti hai jo is dev machine se unreachable hai — bilkul HISO ke
  DMSProxy jaisa masla. Naya API hamesha local dev connection use karta hai, affected nahi hota.

**Real legacy COL ka port**: `http://localhost:2003` (ERMS/COL same IIS Express project share karte
hain), route pattern `{controller}/{action}` (e.g. `/COL/Authenticate`, `/COL/GetCurrentPatientData`).
COL ka credential body JSON hai (ERMS jaisa XML nahi): `{"Username":...,"Password":...,"PatientId":...,"EncounterId":...}`.
`GetDcrptValue` helper: agar value pure numeric hai to plaintext passthrough (encryption skip), warna
decrypt try karta hai.

## 6. COL SaveInvoice — root cause mil gaya, lekin abhi unresolved (real value/DB access chahiye)

Real legacy source (`COLController.cs:465`) se exact request shape nikali — naya API ka
`ColSaveInvoiceRequest` already match karta hai. Us sahi shape se test karne par 400 shape-error
khatam ho gaya, request `[OnlineClaim].[uspInsertUpdateService]` tak sahi pahunchti hai.

**Asal masla** (`sqlcmd` se seedha SP call kar ke dhoonda, dono APIs ko bypass kar ke):
`[Billing].[tblMasterSubService].InsertedBy` column `NOT NULL` hai. Practice 901 ke paas real,
existing services hain (e.g. `"Medical Examination"`/`"#KF005"`, `InsertedBy=57789`), lekin **"COL"
master-service scope mein is practice ka koi record pehle se nahi hai** — isliye har call (chahe
bilkul real existing name/code use karo) SP ke INSERT branch mein jata hai, jo `InsertedBy` na milne
ki wajah se fail hota hai. Ye value na legacy ke C# call signature mein hai, na naye API mein — dono
identical parameters bhejte hain. SP ka poora definition/parameter-list dekhne ki permission nahi hai
(`sys.parameters`/`sys.types`/`sys.procedures` sab access-denied is DB user ke liye).

**Try kiya lekin kaam nahi kiya**: `@pInsertedBy`/`@pUserID`/`@pUserId`/`@pInsertedByID` jaise extra
parameter names SP ko pass karna — SP clearly bolta hai "too many arguments specified", matlab SP
khud koi aisa parameter accept hi nahi karta jahan hum InsertedBy directly de sakein. Ye value SP ke
andar hi kisi internal mechanism se aani chahiye (session context, trigger, ya kuch aur) jo humein
nahi pata.

**Zohaib ne "1 rakh do" bola** — maine puncha kis field mein (Userid? AccountHolderID? kahin aur?)
lekin jawab se pehle hi ye handoff request aa gaya. **Kal ka pehla kaam: ye clarify karo aur SaveInvoice
close karo.** Agar "1" InsertedBy ke liye directly kaam nahi karta (upar prove ho chuka hai ke SP
aisa parameter accept nahi karta), to shayad user ka matlab kuch aur ho — poochna zaroori hai, guess
mat karo.

Real DB access: `sqlcmd -S dbserver-local -U pms_nz -P 'pms@@nz' -d PMS_NZ_V2 -C` (ye credentials
already appsettings.Development.local.json mein hain, koi naya secret nahi).
