# Swagger Test Data — Practice 901

Sample requests for testing every endpoint via Swagger (`https://localhost:7236/swagger`), all scoped to
**practice 901** and, where possible, the same real patient (**2459731**) so results are easy to
compare across systems.

Legend: ✅ confirmed working with real data this session · ⚠️ shape confirmed, needs a real credential
you'll need to supply (I don't have it) · — not applicable / no operation exists

---

## 1. New canonical layer (the unified API)

### 1a. Mint a token — `POST /auth/token`
No real credentials needed (dev-mode `Auth:Enabled=false` accepts any non-empty username/password).
Pick the `originScope` for whichever system you want to "be" for the next call.

**HISO** ✅
```json
{ "username": "demo", "password": "demo", "originScope": "Hiso", "patientId": 2459731, "practiceId": "901" }
```

**KARO** ✅
```json
{ "username": "demo", "password": "demo", "originScope": "Karo", "patientId": 2459731, "rawEncounterId": "280210498__901____local" }
```

**ERMS** ✅
```json
{ "username": "demo", "password": "demo", "originScope": "Erms", "patientId": 2459731, "rawEncounterId": "280210498__901____local" }
```

Copy the `token` field from the response, then click the padlock icon in Swagger ("Authorize") and
paste just the token (no `Bearer ` prefix).

### 1b. Canonical resources (same 3 routes work for all 3 tokens above)

| Route | Notes |
|---|---|
| `GET /v1/patients/2459731/demographics` | ✅ all 3 origins |
| `GET /v1/patients/2459731/demographics?fields=firstName,lastName` | ✅ narrows the response |
| `GET /v1/patients/2459731/conditions` | ✅ all 3 origins |
| `GET /v1/patients/2459731/documents` | ✅ all 3 origins |

HISO: patientId, practiceId, firstName, lastName, dateOfBirth

KARO: patientId, practiceId, firstName, lastName, dateOfBirth, dateOfEnrolment, endEnrolmentDate

ERMS: patientId, firstName, lastName, dateOfBirth, encounterId, nhi
---

## 2. Legacy HISO (`/hiso/*`) — session-based, no token/login needed

Real session for practice 901: **`F4FA4398-3906-4A91-AE8E-70AA044E1672`**

### `POST /hiso/getVersion` ✅
```json
{ "sessionKey": "F4FA4398-3906-4A91-AE8E-70AA044E1672" }
```

### `POST /hiso/getDeliveryOptions` ✅
```json
{ "sessionKey": "F4FA4398-3906-4A91-AE8E-70AA044E1672" }
```

### `POST /hiso/getData` ✅ — demographics
```json
{
  "sessionKey": "F4FA4398-3906-4A91-AE8E-70AA044E1672",
  "dataContainer": {
    "formMetaData": { "formInstanceOperationMode": "N" },
    "submittedDataXml": "<dataContainer><section name=\"demographics\"><field name=\"firstName\" conceptName=\"Patient_FirstName\" /><field name=\"lastName\" conceptName=\"Patient_Surname\" /><field name=\"dateOfBirth\" conceptName=\"Patient_DateOfBirth\" /></section></dataContainer>"
  }
}
```

### `POST /hiso/getData` ✅ — conditions (swap the XML)
```json
{
  "sessionKey": "F4FA4398-3906-4A91-AE8E-70AA044E1672",
  "dataContainer": {
    "formMetaData": { "formInstanceOperationMode": "N" },
    "submittedDataXml": "<dataContainer><section name=\"conditions\"><group name=\"problem\" conceptName=\"Patient_Problem\"><field name=\"name\" conceptName=\"Patient_Problem_Description\" /><field name=\"dateRecorded\" conceptName=\"Patient_Problem_DateRecorded\" /></group></section></dataContainer>"
  }
}
```

### `POST /hiso/getFormView` — shape only, not exercised this session
```json
{ "sessionKey": "F4FA4398-3906-4A91-AE8E-70AA044E1672" }
```

### `POST /hiso/saveContainer` / `POST /hiso/processAction` — writes, not covered here (out of scope for read-only test data)

---

## 3. Legacy KARO/HSS (`/karo/*`)

### `GET /karo/ping` ✅ — no auth needed
No body.

### `GET /karo/authenticate` ⚠️ needs a real credential
Real shape (from earlier verified real test, credentials not reproduced here):
```
GET /karo/authenticate?username=hsslive&password=<real password>&patientId=<encrypted>&encounterId=19592581__901__FZZ999-B&userId=<id>&system=hss&pho=<pho code>
```
You'll need the real password Zohaib has for `hsslive` - I don't have it and won't guess it.

### Once authenticated, reads like `GET /karo/demographics`, `/conditions`, etc. ⚠️
Same pattern - needs the real token from a successful `/karo/authenticate` call above, plus the same
`system`/`pho`/`patientId`/`encounterId` query params. Shape is real and already verified working
earlier this project; just needs your real credentials to mint a fresh token.

---

## 4. Legacy ERMS (`/erms/*`)

### `GET /erms/ping` ✅ — no auth needed

### `POST /erms/authenticate` ⚠️ needs a real credential
Same situation as KARO - real shape confirmed working earlier this project, but needs Zohaib's real
ERMS credentials to mint a token; not reproduced here.

### `GET /erms/GetPatientData` and other reads ⚠️
Needs a real bearer token from the authenticate call above.

---

## 5. Legacy Claim Online (`/erms/col/*`)

### `POST /erms/col/authenticate` ⚠️ needs a real credential
Same situation - real shape, needs real COL credentials.

---

## Why some rows say "needs a real credential"

The **new canonical `/auth/token`** is a deliberate demo/testing shortcut (dev-mode `Auth:Enabled=false`)
that accepts any username/password - that's why section 1 is fully fillable by me. The **legacy
`/karo/authenticate`, `/erms/authenticate`, `/erms/col/authenticate`** endpoints call the *real*
production authentication procedure (`[HSS].[uspInsertAndValidateToken]`), which genuinely validates
the password against real production data - I don't have those real passwords and won't invent
placeholder ones that would just fail. If you give me the real credentials, I can fill these in and
verify them live the same way we did for HISO/KARO/ERMS canonical this session.
