# Parity check run log — 2026-07-30

## POST HISO getVersion
**Legacy request:** POST http://localhost:53507/FormSessionService.svc, SOAPAction getVersion, body: <getVersion xmlns="...formsession"><sessionKey>D2C9E798-1BF9-4610-AC16-B2C09744A40E</sessionKey></getVersion>
**Legacy response:** 200, <getVersionResponse><return><application>PMS</application><applicationVersion>1.0</applicationVersion><hisoversion>1</hisoversion></return></getVersionResponse>
**New request:** POST http://localhost:8080/FormSessionService.svc, same SOAPAction/body
**New response:** 200, <getVersionResponse><getVersionResult><GetVersionResponseReturn><application>PMS</application><applicationVersion>1.0</applicationVersion><hisoversion>1</hisoversion></GetVersionResponseReturn></getVersionResult></getVersionResponse>
**Issue found:** Response wrapper element names differ - legacy uses <return>, new API uses <getVersionResult><GetVersionResponseReturn>. Field values match exactly. A client parsing the real legacy shape (<return>) would fail against the new API.

## POST HISO getDeliveryOptions
**Legacy request:** POST http://localhost:53507/FormSessionService.svc, SOAPAction getDeliveryOptions, sessionKey D2C9E798-1BF9-4610-AC16-B2C09744A40E
**Legacy response:** 200, <return><URL/><senderAccount>Testn28n6ujh</senderAccount><senderPassword>1</senderPassword></return>
**New request:** same
**New response:** 200, <getDeliveryOptionsResult><GetDeliveryOptionsResponseReturn/></getDeliveryOptionsResult> - EMPTY, no senderAccount/senderPassword/URL at all
**Issue found:** REAL DATA GAP - new API returns completely empty fields (not just wrapper naming). Legacy returns real senderAccount=Testn28n6ujh, senderPassword=1 from appSettings/config. Needs investigation in GetDeliveryOptionsQuery/appsettings - likely a missing config value (Hiso:SenderAccount etc) not just a wire-shape issue. Also has the same <return> vs <getDeliveryOptionsResult><GetDeliveryOptionsResponseReturn> wrapper mismatch as getVersion.

## Root cause confirmed for getDeliveryOptions gap
`Hiso:UserId`, `Hiso:Password`, `Hiso:Url`, `Hiso:PracticeEdi` are set ONLY in the gitignored
`appsettings.Development.local.json` (dev machine only) - confirmed absent from `appsettings.json`
and from the Docker container's actual environment (`docker exec hekcoreapi-api-1 printenv | grep Hiso`
shows only Dms* keys, none of the delivery-options ones). Since Docker/production run in the
"Production" ASPNETCORE_ENVIRONMENT, the *.local.json dev override never loads there - so this isn't
just a local test artifact, it would reproduce identically in Azure unless these 4 secrets are set as
real env vars/Key Vault entries before cutover. This is a deployment-config gap, not a code bug.

## POST HISO getFormView
**Legacy request:** sessionKey + formInstanceId=a30f8f2e-519d-449b-a194-03a1c3157fa9
**Legacy response:** 500 Fault (formInstanceId doesn't exist in this test data - expected)
**New request:** same
**New response:** 500 Fault "No operation found for specified action" - getFormView is NOT IMPLEMENTED in the SOAP contract at all
**Issue found:** CRITICAL - getFormView endpoint missing entirely from new API's SOAP service, despite LEGACY_PARITY_VALIDATOR.md marking it "present".

## POST HISO processAction (actionId=launchForm)
**Legacy response:** 200, <return><processed>false</processed></return> (matches doc: launchForm is a no-op stub)
**New response:** 200, <processActionResult><ProcessActionResponseReturn><processed>false</processed></ProcessActionResponseReturn></processActionResult>
**Issue found:** Value matches (processed=false, correctly reproduces the no-op stub). Same wrapper-naming pattern mismatch as getVersion/getDeliveryOptions (<return> vs <processActionResult><ProcessActionResponseReturn>).
