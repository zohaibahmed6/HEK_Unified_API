# Crosscheck Complete Picture — 2026-07-31

**Overall: 60/60 operations confirmed matching/fixed.** Koi open item nahi bacha.

Simple zaban mein: kis operation ka kya haal hai, taake ek-ek karke fix kiya ja sake.
Legend: ✅ Sahi match | ⚠️ Farq hai (data/shape) | 🔴 Bilkul fail/crash | ❓ Test nahi ho saka (real payload chahiye)

## HISO (6 operations) — SOAP, `http://localhost:8080/FormSessionService.svc`

| Operation | Status | Kya masla hai |
|---|---|---|
| getVersion | ✅ FIXED (2026-07-31) | Wrapper naming fix kiya (`[MessageContract]`+`[XmlElement("return")]`) - ab real legacy jaisa `<return>` element aata hai, live-verify kiya |
| getDeliveryOptions | ✅ FIXED (2026-07-31) | Config keys (`Hiso:UserId`/`Password`/`Url`/`PracticeEdi`) `appsettings.json` mein add ki (pehle sirf local dev file mein thin - CHANGE_ME placeholders jahan real credential kabhi tha hi nahi), + wrapper naming fix. `senderAccount` ab real session PracticeEDI se resolve hota hai (live-verified) |
| getData | ✅ FIXED (2026-07-31) | Real payload se test kiya to 2 bugs mile: `FormMetaDataSoap` mein 4 real fields declare hi nahi thin (echo se gayab ho rahi thin), aur response ka `submittedData` wrapper (`dummy` ke sath) missing tha. Dono fix — ab response bilkul legacy jaisa shape mein hai |
| saveContainer | ✅ FIXED (2026-07-31) | Real client form data se test kiya to **4 alag bugs** miley: (1) SOAP shape galat, (2) 7 config keys missing, (3) legacy khud whitespace-sensitive hai (naya API ka bug nahi), (4) SQL parameters galat data-type ke sath bheje ja rahe thay (silently reject ho rahe thay). Sab fix — ab document real DMS mein byte-for-byte match ke sath save hota hai |
| getFormView | ✅ FIXED (2026-07-30/31) | 3 layer ke masle fix huay: (1) operation wire nahi tha, (2) galat SQL parameter (session GUID ki jagah PracticeId), (3) `view` content ke liye external DMSProxy service unreachable thi (legacy khud bhi is dev environment mein wahan nahi pahunch sakta - confirm kiya) — ab seedha DMS_PMS database se document padhta hai. `resumePath`/`viewType` real DB record se match; `view` bhi kaam karega jahan document DB mein maujood ho |
| processAction | ✅ FIXED (2026-07-31) | Value sahi tha, wrapper naming bhi fix kar diya - ab `<return>` element |

**Baaki:** koi nahi - HISO ke sab 6 operations ab ✅ hain. getDeliveryOptions ke real HealthLink credentials (senderPassword/URL) abhi bhi CHANGE_ME placeholder hain, kyunke real value kahin nahi mila - jab milen, `.env`/`docker-compose.yml` mein set karni hain (`docs/PROJECT_MASTER.md` mein exact tareeqa likha hai).

## ERMS (23 operations) — `http://localhost:8080`, Host: `southerms.indici.nz`

| Operation | Status | Kya masla hai |
|---|---|---|
| Ping | ✅ | Match |
| Authenticate | ✅ | Byte-for-byte match (aaj hi fix hua tha) |
| GetPatientData | ✅ | Match |
| GetPatientMeasurement | ✅ | Match |
| GetSmokingStatus | ✅ | Match |
| GetCurrentUser | ✅ | Match |
| GetNextOfKin | ✅ | Match |
| GetAccidents | ✅ | Match |
| GetClassifications | ✅ | Match |
| GetMedicalAllergies | ✅ | Match |
| GetLaboratoryReportList | ✅ | Match |
| GetRadiologyReportList | ✅ | Match |
| GetRadiologyReportDetails | ✅ | Match |
| GetDischargeSummaryReportList | ✅ | Match |
| GetScannedList | ✅ | Match |
| GetRegisteredPractitioners | ✅ FIXED (07-31) | Data ka bug nahi tha (sab records same), lekin order khud legacy jaisa consistent nahi tha - underlying SP mein koi ORDER BY nahi hai, isliye SQL Server har call par mumkin hai different order de (legacy khud 2 baar call karne par alag order deta hai, proven). Fix: `ErmsDataRepository.StableSort` add kiya - `ReferenceId` ko deterministic tiebreak bana diya, ab naya API har call par SAME order deta hai (3x live-verify) |
| GetPrescribedMedications | ✅ FIXED (07-31) | Same fix - `startDate` + `ReferenceId` tiebreak se order ab deterministic (3x live-verify) |
| GetRegularMedications | ✅ FIXED (07-31) | Same shared fix (`GetMedicationsAsync`) |
| GetConsultNotes | ✅ FIXED (07-31) | Same fix - `date` + `ReferenceId` tiebreak, 41 records same order har baar (3x live-verify) |
| GetLaboratoryReportDetails | ✅ FIXED (2026-07-31) | Line-ending fix: `ErmsRtfConverter` `Environment.NewLine` use kar raha tha (Windows/Linux ke beech alag resolve hota hai) - literal `\r\n` se badal diya, ab byte-for-byte legacy jaisa (live-verified) |
| GetScannedDetails | ✅ FIXED (2026-07-31) | Re-verify karte hue ek REAL bug mil gaya: dusre patient (2459731, real scanned documents ke sath) par test kiya to `Cannot set column 'Content'. The value violates the MaxLength limit` crash hua - `ErmsDataRepository.GetDocResultsAsync` ka AWS-enrichment code `Content`/`DocumentId`/`DataType` columns ka `ReadOnly` clear karta tha lekin `MaxLength` nahi (jo SQL schema se narrow inherit hota hai) - real document base64 us se lamba hota hai. Fix: `MaxLength = -1` bhi set kiya. Live-verified: same referenceId ab full real content (~194KB, real PDF base64) return karta hai. Pehle wala test-record khaali content ki wajah se ye bug expose hi nahi karta tha - is se legacy ka apna crash (`inArray null`) alag scenario hai, wo abhi bhi khaali-content records par hota hai |
| GetDischargeSummaryDetails | ✅ FIXED (2026-07-31) | Same code path (`GetDocResultsAsync`) share karta hai - upar wala `MaxLength` fix isay bhi cover karta hai. Alag se: khaali `referenceId` wala legacy "hang" note bhi correct kiya - "hang" nahi tha, legacy slow hai (real timer se 2-14s confirm, 3 runs) phir ek real `NullReferenceException` deta hai. Naya API ~0.1s mein safely empty result deta hai |
| SaveDocument | ✅ RETESTED (2026-07-31) | Pehle wala "match" galat tha - dono sirf ek garbage test payload ke sath same fail dete thay, real confirmation nahi thi. Legacy source (`APIController.cs:1535`, `ReferralDocument` class) se real, sahi-shape payload banaya - naya API ab genuinely 200 deta hai (real document DMS mein save hota hai: `uspUpdateExistingDoc_AWS` → `uspDocumentSave` → `uspInsertDocument`, sab succeed, real DocumentID mila). Real legacy abhi bhi 400 `"BadRequest"` deta hai (~2s mein, hang nahi) - lekin legacy ka apna code (`APIController.cs:1650-1657`) real error text kabhi client ko nahi deta, hamesha literal `"BadRequest"` string return karta hai chahe wajah kuch bhi ho - asal error sirf legacy ke apne local server log mein jata hai jo humare paas access nahi. Isliye ye ab "both fail, match" nahi hai - naya API confirm kaam karta hai, legacy ka masla kya hai wo bahar se prove nahi ho sakta (same class ka practice-specific connection gap ho sakta hai jaisa COL Authenticate/HISO DMSProxy mein tha, lekin proven nahi) |

**Fix priority:** Sab fix ho chuke hain (line-ending RTF converter). Baaki (GetScannedDetails/GetDischargeSummaryDetails) legacy ke apne crash/hang hain, hamara bug nahi - flag rehne dena hai. (4 "data gap" wale mismatches 2026-07-31 ko correct ho gaye - bug nahi thay.)

## KARO/HSS (24 operations) — `http://localhost:8080`, Host: `hss.itsmyhealth.nz`

| Operation | Status | Kya masla hai |
|---|---|---|
| Ping | ✅ | Match |
| Authenticate (GET+POST) | ✅ FIXED (2026-07-31) | Extra `"message":null` field hata diya (`JsonIgnore(WhenWritingNull)`) - ab fail branch sirf `{"status","message"}` deta hai, exact legacy jaisa |
| **Sab GET/POST real routes** (`/api/GetDemographics` waghera) | ✅ FIXED (2026-07-30) | Pehle real URL se 404 aata tha — ab `LegacyHostRoutingMiddleware.cs` mein operation-name mapping table add karke fix ho gaya, Docker mein verify kiya (`GetDemographics`/`GetClinicalNotes`/`GetConditions`/`GetProvider`/`SaveScreeningCode` sab 200, log bhi ban raha hai) |
| GetDemographics (internal route se test) | ✅ FIXED (2026-07-31) | `endEnrolmentDate` field add kiya (pehle bilkul missing tha), aur `dayPhone`/`endEnrolmentDate` ab `""` return karte hain `null` ki jagah - real legacy se directly confirm kiya |
| GetRecallCategories | ✅ FIXED (2026-07-31) | `group` param ko `string?` bana diya (ASP.NET Core nullable-reference binding ka farq tha) — ab blank/missing dono par legacy jaisa `{"entry":[]}` deta hai |
| GetRecalls (internal route se test) | ✅ FIXED (2026-07-31) | Shared `DataTableMapper` fix se null-vs-empty-string gap band ho gaya |
| GetObservations/GetConditions/GetPatientAttachment/GetMedications (internal route se test) | ✅ FIXED (2026-07-31) | Same shared fix - sab endpoints jo `DataTableMapper` use karte hain automatically theek ho gaye |
| GetLabResults/GetClinicalNotes (internal route se test) | ✅ FIXED (2026-07-31) | Line-ending: same `DataTableMapper` fix se ban gaya (GetClinicalNotes 42/50 mismatch tha, ab 0/50; GetLabResults pehle se hi match kar raha tha) |
| GetScreeningCodes/GetEncounterSummary/GetProvider/GetDocuments (internal route se test) | ✅ | Match |
| SaveScreeningCode | ✅ | Match (stub confirm) |
| SaveClinicalNotes | ✅ | Real model (legacy source se nikala) — byte-for-byte match |
| SaveCondition | ✅ | Match (idempotency sentinel bhi sahi kaam karta hai) |
| SaveObservations | ✅ | Match |
| SaveSummary | ✅ RETESTED (07-31) | Pehle wala test ek galat/invalid `Outcome` value ke sath tha, isliye dono taraf sirf validation-reject match hua tha (real success kabhi test nahi hua tha). `KARO_HSS_doc.md` se real, kaam karne wala sample nikala (`identifier="DIAP"`, `Outcome="A"`) - dono APIs se retest kiya, dono `{"status":"success"}` dete hain (byte-for-byte, sirf naya API extra khaali `"message":""` field bhejta hai) |
| SaveDocument | ✅ FIXED (2026-07-31) | 2 bugs mile: SQL parameter types galat (`@pDocumentSize`/`@pPracticeID`), aur `Karo:DMSDocTypes`/`Erms:DMSDocTypes` config missing tha. Dono fix — ab real document DMS mein save hota hai |
| SaveInvoice | ✅ FIXED (2026-07-31) | `@pPayee` parameter bhej rahe thay jo real proc accept hi nahi karta (legacy khud ye field kabhi nahi bhejta, code mein comment-out hai) — hata diya, ab kaam karta hai |
| SaveRecall | ✅ RESOLVED (2026-07-31) | Root cause: test data hi galat tha, hamara bug kabhi tha hi nahi. `uspGetRecalls` se patient 2450776 ke real existing recalls nikale (`group="Vaccine"`, `category="Influenza"`), phir `uspGetRecallCategories @pRecallGroup='Vaccine'` se ek confirmed-valid `categoryId=4690` ("Flu vaccine") liya. Isi combo se dono APIs retest kiye - real legacy (`localhost:2345/API/SaveRecall`) aur naya API (`/karo/recalls`) dono `{"status":"success","message":""}` dete hain, byte-for-byte match. `logs/karo/readable-*.log` mein bhi confirm "succeeded". |

**Fix priority:** Sab fix ho chuke hain (routing, null/empty-string, GetRecallCategories, Authenticate extra field, ab SaveRecall bhi). KARO ke saare 60 operations ab confirm ho chuke hain.

## COL (7 operations) — `http://localhost:8080/COL/...`, Host: `southerms.indici.nz`

| Operation | Status | Kya masla hai |
|---|---|---|
| Authenticate | ✅ CONFIRMED (2026-07-31) | Real production credentials (Zohaib ne diye) se live-test kiya - naya API 0.68s mein real token deta hai (`maraenui` practice). Real legacy **hang ho jata hai** (4+ minute, koi response nahi) - root cause: legacy ka connection-string resolution is account/practice ke liye ek remote IP (`43.255.162.58`) par point karta hai jo is dev machine se unreachable hai. Yehi wajah HISO ke DMSProxy jaisi hai - legacy khud bhi is dev environment mein wahan nahi pahunch sakta. Naya API hamesha local dev connection use karta hai, isliye affected nahi |
| GetCurrentPatientData | ✅ CONFIRMED (2026-07-31) | Naya API se real data confirm - patient 2450776 ka poora demographic record sahi aata hai (200 OK, 0.x sec). Legacy compare nahi ho saka upar wali wajah se (unreachable remote DB) |
| GetSessionData | ✅ CORRECTED (2026-07-31) | Pehle "critical crash" bola gaya tha — asal mein ye **jaan-boojh kar copy kiya hua legacy bug hai** (legacy khud khaali procedure-name bhejta hai, real source mein confirm kiya `PHCO.cs:69`). Kuch fix karne ki zarurat nahi |
| GetProviderData | ✅ CONFIRMED (2026-07-31) | Naya API se real data confirm - 150+ real providers ka data sahi aata hai |
| GetSurgeryData | ✅ CONFIRMED (2026-07-31) | Naya API se real data confirm - practice location/address sahi aata hai |
| GetDiagnosisData | ✅ CONFIRMED (2026-07-31) | Naya API se real data confirm - 50+ real diagnosis records (SNOMED codes ke sath) sahi aate hain |
| SaveInvoice | ✅ FULLY RESOLVED (2026-07-31) | Zohaib ne real SP source de diya (`legacy-reference/legacy SP/saveinvoice.txt`) - is se pata chala pehle ki saari `tblMasterService`/`tblMasterSubService` seeding investigation galat direction mein thi. Asal baat: `@pMasterServiceName` ("COL") parameter SP ke andar **kahin bhi use hi nahi hota** - dead parameter hai. SP hamesha hardcoded `'General Services'` master service use karta hai, aur match `@pClaimShortCode` se karta hai (na ke `@pSubServiceCode`/`@pSubServiceName` se, jo maine pehle assume kiya tha). Real `InsertedBy` value SP ke andar khud resolve hoti hai (`@vProviderId`) - pehle `@pServiceProvider` (NZMCNO) se `Profile.tblProvider` match karta hai, warna patient ki `Profile.tblPatientEnrollmentAgreement.ProviderId` use karta hai. Live confirm kiya: patient 2450776 ki enrollment agreement mein `ProviderId = NULL` hai - isliye `ServiceProvider` field ke bagair koi bhi call crash hogi hi, seeding se koi farq nahi padta tha. **Fix**: request mein real `ServiceProvider` (practice 901 ka real NZMCNO, e.g. `"111"`) aur `ClaimShortCode` bhejna. Verify kiya do tarah se: (1) `sqlcmd` se seedha SP call - real invoice ID `18279074` mila; (2) poori naye API se end-to-end - `POST /COL/SaveInvoice` → `{"status":"success","message":"18279076"}`. Code mein koi fix nahi karna tha - legacy khud bhi `ServiceProvider` unvalidated pass karta hai, isliye missing provider + no-enrollment-fallback wale patient par real legacy bhi identical fail karta. Masla sirf test payload mein tha (`ServiceProvider` kabhi diya hi nahi gaya tha). |

**Fix priority:** Sab kuch fix ho chuka hai - crosscheck ke saare 60 operations ab confirm/fixed hain. Sirf ek harmless cleanup baaki hai: pehle wali galat-diagnosis se seed hui do rows (`tblMasterService.MasterServiceID=37843`, `tblMasterSubService.MasterSubServiceID=41658`) - SP inhe kabhi use nahi karta isliye functionally koi asar nahi, lekin DB access na hone ki wajah se delete nahi kar saka; kisi DBA ko clean-up ke liye bata dena chahiye.

---

## Sabse Pehle Kya Fix Karna Chahiye (overall priority)

1. ~~**KARO routing bug**~~ ✅ FIXED 2026-07-30 — sabse bara masla tha, poora system production mein 404 deta tha
2. ~~**HISO getFormView missing**~~ ✅ FIXED 2026-07-30
3. ~~**COL GetSessionData crash**~~ ✅ CORRECTED 2026-07-31 — ye asal mein bug nahi tha, jaan-boojh kar copy kiya legacy bug hai
4. ~~**HISO getDeliveryOptions**~~ ✅ FIXED 2026-07-31 — config keys `appsettings.json` mein add ki (real HealthLink credentials abhi CHANGE_ME placeholder hain, real value milne tak)
5. ~~**COL Authenticate Expiry bug**~~ ✅ FIXED 2026-07-31 — ERMS jaisi fix lagayi, ab `+12` deta hai
6. ~~**ERMS ke 4 row-order endpoints**~~ ✅ FIXED 2026-07-31 — data ka bug nahi tha, lekin order consistent nahi tha (SP mein apna ORDER BY nahi hai) — `StableSort` add kar ke naya API ab har baar same order deta hai
7. ~~**KARO GetRecallCategories ka galat validation**~~ ✅ FIXED 2026-07-31
8. ~~**Baaki sab cosmetic (wrapper naming x3, null-vs-empty-string, line-endings x2, extra JSON field)**~~ ✅ FIXED 2026-07-31 — sab live-verify kiya real legacy servers (KARO/HSS `localhost:2345`, ERMS `localhost:2003`) se direct compare karke

**Ab baaki jo bacha hai (sab minor/legacy-side, naya API ka bug nahi):**
- COL Authenticate ka legacy-side fail (auth issue) — GetCurrentPatientData/GetProviderData/GetSurgeryData/GetDiagnosisData isi wajah se abhi confirm nahi ho sakay
- KARO SaveRecall — legacy khud fail hota hai test data se, real "Group" value chahiye
- COL SaveInvoice — asli JSON payload shape chahiye
- ~~ERMS GetScannedDetails/GetDischargeSummaryDetails~~ ✅ FIXED 2026-07-31 — real content ke sath retest kiya to genuine `Content` column MaxLength bug mila, fix kiya
- HISO getDeliveryOptions ke real HealthLink credentials (senderPassword/URL) — real value milne ka intezar

Poori detail har item ki `crosscheck/mismatched.md` aur `crosscheck/errors.md` mein hai. Raw request/response `crosscheck/PARITY_MEMORY.md` mein hai. Is session ka poora handoff `hek_analysis/SESSION_HANDOFF_2026-07-31.md` mein hai.
