> ![C:\\Users\\abdullah.noor\\Downloads\\Vaentia-Logo700X245.png](media/image1.png)

Indici *Health Systems Solutions* Web API Specification

| Created Date | 06/06/2018 |
| ------------ | ---------- |
| Updated Date | 03/05/2021 |
| Author       | Abdullah   |
| Version      | 2.1.3      |

**  
**

# Table of Contents

[1. Document Details 4](#document-details)

[1.1 Version 4](#version)

[2. Purpose 7](#purpose)

[3. Overview 7](#overview)

[4. Flow Diagram 7](#flow-diagram)

[4.1 Authentication 8](#authentication)

[4.2 Patient ID AND Encounter ID & Others 8](#patient-id-and-encounter-id-others)

[4.2.1 Patient ID 8](#patient-id)

[4.2.2 Encounter ID 8](#encounter-id)

[4.2.3 User ID 9](#user-id)

[4.3 Invocation 9](#invocation)

[4.3.1 Health Systems Solutions (HSS) portal 9](#health-systems-solutions-hss-portal)

[4.3.2 Indici HSS Web API 9](#indici-hss-web-api)

[4.3.3 Indici PMS System Access 9](#indici-pms-system-access)

[4.3.4 Sample Code (Javascript) 10](#sample-code-javascript)

[5. Test Data 11](#test-data)

[6. Categories of Data Requests 11](#categories-of-data-requests)

[6.1 Ping 11](#ping)

[6.1.1 Get 11](#get)

[6.2 Authenticate 11](#authenticate)

[6.2.1 Post 11](#post)

[6.3 Demographics 12](#demographics)

[6.3.1 Get 12](#get-1)

[6.4 Provider 13](#provider)

[6.4.1 Get 13](#get-2)

[6.5 Medications 14](#medications)

[6.5.1 Get 14](#get-3)

[6.6 Clinical Notes 14](#clinical-notes)

[6.6.1 Get 15](#get-4)

[6.6.2 Post 15](#post-1)

[6.7 Screening Codes 16](#screening-codes)

[6.7.1 Get 16](#get-5)

[6.8 Encouter Summary 17](#encouter-summary)

[6.8.1 Diabetes Project – POST 17](#diabetes-project-post)

[6.8.2 Diabetes Foot Examination- POST 18](#_Toc2070533)

[6.8.3 Retinopathy - POST 18](#_Toc2070533)

6.8.4 Point of care Test Result 18

6.8.5 NZ Early Watning Score 18

6.8.6 Oxygen Saturation 18

6.8.7 PoC Outcomes 18

6.8.8 PoC Troponin 2 Hours 18

6.8.9 PoC Troponin 0 Hours 18

6.8.10 ED Assessment of Chest Pain Score 18

[6.9 Lab Results 18](#lab-results)

[6.9.1 Get 18](#get-6)

[6.10 Inbox Documents 19](#inbox-documents)

[6.10.1 Get - Collection 20](#get---collection)

[6.10.2 Get – Single Document 20](#get-single-document)

[6.10.3 Post –Document 21](#post-document)

[6.11 Observations 21](#observations)

[6.11.1 Get 21](#get-7)

[6.11.2 Post 23](#post-2)

[6.12 Conditions/Classifications 24](#conditionsclassifications)

[6.12.1 Get 24](#get-8)

[6.12.2 Post 25](#post-3)

[6.13 Recalls 26](#recalls)

[6.13.1 Get 26](#get-9)

[6.13.2 Post 27](#post-4)

[6.14 Recall Categories 28](#recall-categories)

[6.14.1 Get 28](#get-10)

[6.15 Invoice 28](#invoice)

[6.15.1 Post 28](#post-5)

# 1\. Document Details

## Version

<table>
<thead>
<tr class="header">
<th>Date</th>
<th>Version</th>
<th>Author/Reviewer</th>
<th>Organisation</th>
<th>Changes Made</th>
</tr>
</thead>
<tbody>
<tr class="odd">
<td>06/06/2018</td>
<td>1.0</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>First draft created.</p></li>
</ul></td>
</tr>
<tr class="even">
<td>11/06/2018</td>
<td>1.1</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Updated HSS Web API URLs</p></li>
<li><p>New querystring to identify PHO</p></li>
<li><p>New ping method/operation</p></li>
<li><p>Test data section introduced</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>14/06/2018</td>
<td>1.2</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Added Authenticate operation</p></li>
<li><p>Updated query string values</p></li>
<li><p>Added flow diagram</p></li>
</ul></td>
</tr>
<tr class="even">
<td>19/06/2018</td>
<td>1.3</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Updated Flow diagram (corrections)</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>25/06/2018</td>
<td>1.4</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Modified Authenticate operation</p></li>
<li><p>Updated Authenticate URI</p></li>
<li><p>Updated Test Data section</p></li>
</ul></td>
</tr>
<tr class="even">
<td>28/06/2018</td>
<td>1.5</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Updated Authenticate Response</p></li>
<li><p>Updated Patient/Encounter IDs</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>05/07/2018</td>
<td>1.6</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Updated Patient and Encounter IDs</p></li>
</ul></td>
</tr>
<tr class="even">
<td>12/07/2018</td>
<td>1.7</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Updated Indici portal access URL</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>03/09/2018</td>
<td>1.8</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New operation GetProvider</p></li>
<li><p>Updates to fields for GetDemographics</p></li>
<li><p>Return integer 0 handling change</p></li>
</ul></td>
</tr>
<tr class="even">
<td>07/09/2018</td>
<td>1.9</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New operations: GetMedications, GetClinicalNotes, GetScreeningCodes</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>12/09/2018</td>
<td>1.9.1</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New operation: GetLabResults</p></li>
<li><p>Version numbering correction</p></li>
</ul></td>
</tr>
<tr class="even">
<td>14/09/2018</td>
<td>2.0.1</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>POST operations: SaveClinicalNotes, SaveScreeningCodes</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>25/09/2018</td>
<td>2.0.2</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Change of authentication function from GET to POST</p></li>
<li><p>Change of all calls to acquire authorization token from auth Header</p></li>
</ul></td>
</tr>
<tr class="even">
<td>27/09/2018</td>
<td>2.0.3</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New operations: GetDocuments (all) &amp; GetDocuments (single)</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>12/10/2018</td>
<td>2.0.4</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New operations: GetConditions, GetObservations</p></li>
<li><p>Updated operations: GetDemographics, GetProvider</p></li>
</ul></td>
</tr>
<tr class="even">
<td>23/10/2018</td>
<td>2.0.5</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New operation: SaveDocument<br />
Updated operation: GetObservations, GetConditions</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>07/12/2018</td>
<td>2.0.6</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Added Success/Fail responses to SaveDocument operation</p></li>
</ul></td>
</tr>
<tr class="even">
<td>25/01/2019</td>
<td>2.0.7</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New parameter to identify out/in documents in operation SaveDocument</p></li>
<li><p>Updated/corrected saveClinicalNotes post sample</p></li>
<li><p>Intro of <em>User Id</em> with Invocation URL</p></li>
<li><p>Updated <em>Encounter ID</em> in calls</p></li>
<li><p>Updated saveDocument post sample</p></li>
<li><p>Intro of User Id in <em>GetProvider</em> operation</p></li>
<li><p>Updated <em>GetProvider</em> get sample</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>31/01/2019</td>
<td>2.0.8</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New Operation: Recalls</p></li>
<li><p>Split address in Demographics to street, suburb, city and postcode</p></li>
</ul></td>
</tr>
<tr class="even">
<td>12/02/2019</td>
<td>2.0.9</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Introduced userId in saveClinicalNotes, saveDocument</p></li>
<li><p>New Operations: saveRecall</p></li>
<li><p>Introduced medicineName property in getMedications</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>14/02/2019</td>
<td>2.1.0</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New Operations: saveObservations, saveCondition, getRecallCategories</p></li>
<li><p>Correction: resourceType value in sample JSON for saveRecall</p></li>
<li><p>Addition of properties in getCondition: fsn, type</p></li>
<li><p>Changed category to categoryId in saveRecall</p></li>
</ul></td>
</tr>
<tr class="even">
<td>19/02/2019</td>
<td>2.1.1</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>New observation values to save (CVRA) in saveObservations operation</p></li>
<li><p>Added URI for production HSS API</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>22/02/2019</td>
<td>2.1.2</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>saveInvoice sample JSON provided</p></li>
</ul></td>
</tr>
<tr class="even">
<td>03-05-2021</td>
<td>2.1.3</td>
<td>imran rashid</td>
<td>Valentia Technologies</td>
<td><blockquote>
<p>Enrolment details added in Getdemographic API "dateOfEnrolment" and `"endEnrolmentDate"</p>
</blockquote></td>
</tr>
</tbody>
</table>

# Purpose

The purpose of this document is to give an overview of the Web API that defines the data exchanged between the *Indici Practice Management System* and *Health Systems Solutions (HSS)* portal. It lists the operations exposed to be consumed by each side.

# Overview

This document specifies all the data required to be read and/or written back from Indici Practice Management System. While based on *FHIR* concepts in some places, this specification is a far simpler system than a full implementation of FHIR. This also details the scope and implementation of authentication between the two and subsequent authorization of each API call.

*JSON* parameters and objects use camel case and are case sensitive.

# Flow Diagram

![C:\\Users\\ABDULL\~1.NOO\\AppData\\Local\\Temp\\SNAGHTML3e01d5.PNG](media/image2.png)

## 

## Authentication

Each API call requires authentication. The guiding principle is that an authentication string needs to be sent with each API call. This authentication string is the token initially generated in the first “hand-shake” call to the Web API.

The first call posted should include a user name and password (provided by the vendor)~~, or some other similar form of data.~~ ~~Individual vendors can specify different ways of specifying or obtaining the token, but the mandated method for supplying the authentication is either as a parameter to the API web service or preferably included in the header of the HTTPS request as specified by the vendor for example in the Authorization request header field.~~

The Authorization token obtained after successful authentication will be used in all the subsequent function calls of the API. The delivery of the token will be in the Authorization of yj http header.

The token has a set expiry which is included in each subsequent Authenticate call response.

## Patient ID AND Encounter ID & Others

### Patient ID

Each vendor will provide a patient identifier that is unique across their system. In this case, Indici will provide the ID in the invocation call as a parameter and the result query parameter needs to be parsed by *HSS* and sent to the Web API with the call for authentication. This ID is string. NHI is not satisfactory for this purpose because not every patient that is seen will have an NHI.

### Encounter ID

The encounter ID is a mechanism that allows the Indici PMS to associate data with a particular consultation and provider.

An encounter ID string is included in each API call if required. If not required then it would be omitted from the query string or posted payload. The initial call should have the value passed to *HSS* portal for it to pass in the subsequent calls to the Web API.

It is up to the PMS to maintain the appropriate connection between encounter ID and patient/provider. The data associated with an encounter ID should be the provider who launched the portal using the designated button/link.

### User ID

The User Id is supplied to the invocation URL to HSS form. This is the Identifier that uniquely identifies the current logged in user/practitioner. This information needs to be supplied to the Provider operation for further action.

## Invocation

### Health Systems Solutions (HSS) portal

Browser-based Indici PMS will open the HSS portal through a designated link or button. The called URL will include the patient relevant information like the *PatientId* and *EncouterId* as query string parameters.

The invocation URL used will consist of

1.  Host:
    
    1.  Development: [**http://localhost:44300/Account/Authenticate?Pms=Indici**](http://localhost:44300/Account/Authenticate?Pms=Indici)
    
    2.  Production x 5 PHO: **}**

2.  Query strings:
    
    1.  Patient ID: **\&patientId=941286**
    
    2.  Encounter ID: **\&encounterId=13782258**
    
    3.  User ID**: \&userId=941287**

<!-- end list -->

1.  
2.  
### 

### Indici HSS Web API

The Web API URL used will consist of

1.  Host:
    
    1.  Development: **[https://devhss.itsmyhealth.nz/api/{Operation-Name](https://devhss.itsmyhealth.nz/api/%7bOperation-Name)}**
    
    2.  Production: **[https://hss.itsmyhealth.nz/api/{Operation-Name](https://hss.itsmyhealth.nz/api/%7bOperation-Name)}**

2.  Query strings:
    
    1.  **\&system=hss**
    
    2.  **\&pho=NBPH**
    
    3.  See each call below

### Indici PMS System Access

The Web portal for Indici PMS (limited) can be accessed:

URL: <https://pmstraining.itsmyhealth.nz:444>

<span class="underline">Username</span>: hssadmin  
<span class="underline">Password</span>: \*\*\*

Patient: Patient HSS (you click on the name)

<span class="underline">Note</span>: The Patient Consult page opens in a new popup/window. Make sure you have disabled your popup-blocker for the site.

HSS Icon:

![](media/image3.png)

### Sample Code (Javascript)

function CallWebAPI() {

var xhttp = new XMLHttpRequest();

xhttp.onreadystatechange = function() {

if (this.readyState == 4 && this.status == 200) {

//JSON.Parse(this.responseText));

//your parsing code

}};

xhttp.open("GET", " https://devhss.itsmyhealth.nz/api/{Operation-Name}", true);

xhttp.setRequestHeader("Content-type", "application/json");

xhttp.setRequestHeader("Authorization", "\[Auth Token\]");

xhttp.send();

}

# Test Data

Return values for each of the operation requests below are dummy/test data ~~including the authorization code~~. This is for the purpose of keeping the data consistent to develop the required parsers. On request, the data will be switched to the dynamic collection of the development/training environment.

Request for sample data and multiple patients has been entertained.

# Categories of Data Requests

## Ping

API requirements: GET (read only)

### Get

Returns status (up) of the API.

[**/ping**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

"status": "success\!"

}

## Authenticate

API requirements: POST

<span class="underline">Properties</span>:

1.  username = *staginghss*

2.  password = *\*\*\**

3.  patientId = 1950057

4.  encounterId = 28999606\_491

5.  system=hss

6.  pho=NBPH

### Post

Returns a generated token when correct credentials are provided (string). The token is used for authentication in the subsequent calls

**/authenticate**

{

"Username":"staginghss",

"Password":"\*\*\*\*\*\*\*\*\*\*",

"PatientId":"1950057",

"EncounterId":"28999606\_491",

"system":"hss",

"pho":"NBPH"

}

<span class="underline">SUCCESS RESPONSE:</span>

{

"status": "success",

"token": "5895CBDF-AB10-4A22-99F8-DC26E372104B",

"expiry": "2020-10-29T09:45:43",

"practiceId": "demo"

}

<span class="underline">FAIL RESPONSE:</span>

{

"status": "fail",

"message": "Authentication failed\!"

}

## Demographics

API requirements: GET (read only)

### Get

Returns current demographic data for the patient. Where no return data is available, either null or "" is used, and both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

[/GetDemographics?patientId=2573982\&encounterId=29000845\_491\&system=hss\&pho=nbph](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

"patientId": "2573982",

"resourceType": "Patient",

"system": "hss",

"entry": \[

{

"listDemographicInfo": \[

{

"nhi": "ZZZ0023",

"birthDate": "1995-11-04T00:00:00",

"type": "Patient",

"titleCode": "Miss",

"given": "Amy",

"family": "MOUSE",

"gender": "female",

"ethnicity1": 11,

"ethnicity2": 0,

"ethnicity3": 0,

"quintile": "4",

"meshblock": "2345700",

"cellNumber": "+642112345678",

"dayPhone": "",

"email": "xxf@gmail.com",

"fullAddress": "888 Su Road, Stratford 5555",

"enrolmentStatus": "Un-enroled",

"smokingStatus": "Never Smoked",

"street": "888 Su Road",

"suburb": "",

"city": "Stratford",

"postCode": "5555",

"isnzResident": "T",

> "dateOfEnrolment",

"endEnrolmentDate"

}

\],

"listcardtype": \[\]

}

\]

}

## Provider

API requirements: GET (read only)

### Get

Returns current patient’s provider data. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

[**/getProvider?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13782258\_491\&userId=941287**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

"patientId": "941286",

"resourceType": "Provider",

"system": "hss",

"entry": \[

{

"type": "HSS",

"nzmc": "NZM100",

"birthDate": "1970-01-01",

"titleCode": "Mr",

"given": "Hss",

"family": "Admin",

"gender": "male",

"dayPhone": "+6421222222",

"email": "hss@abc.com"

}

\]

}

## Medications

API requirements: GET (read only)

### Get

Returns current patient’s medications. The division of long and short term medications is by a Boolean value. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

**/getMedications?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13782258\_491**

{

"patientId": "941286",

"resourceType": "Medications",

"system": "hss",

"entry": \[

{

"sctid": "10037311000116101",

"medicineName": "paracetamol 500 mg tablet",

"dosage": "1",

"route": "Oral",

"expectedDuration": "10",

"startDate": "2018-09-07",

"isLongterm": "false",

"directions": "Take 1 tab(s) Weekly"

},

{

"sctid": "45368761000116101",

"medicineName": "ibrutinib 140 mg capsule",

"dosage": "1",

"route": "Oral",

"expectedDuration": "20",

"startDate": "2018-11-29",

"isLongterm": "true",

"directions": "Take 1 cap(s) Twice Weekly"

}

\]

}

## Clinical Notes

API requirements: GET (read only)

### Get

Returns current patient’s encounter clinical notes data. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

[**/getClinicalNotes?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13782258\_491**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

"patientId": "941286",

"resourceType": "Notes",

"system": "hss",

"entry": \[

{

"subjectiveNotes": "This is subjective comment",

"objectiveNotes": "This is objective comment",

"assessment": "",

"plans": "",

"appointmentAdvice": "",

"date": "2018-11-30T22:43:32"

},

{

"subjectiveNotes": "Test subjective 2",

"objectiveNotes": "Test objective 2",

"assessment": "Test assessment 2",

"plans": "Test plan 2",

"appointmentAdvice": "",

"date": "2019-01-26T00:23:56"

}

\]

}

### Post

To post a JSON to save clinical notes against the current patient’s encounter. Successful save will return a success message (JSON) and a failure will return the error.

[**/saveClinicalNotes**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

"patientId": "941286",

"encounterId": "13782258",

"userId": "",

"resourceType":"ClinicalNotes",

"system": "hss",

"subjectiveNotes": "Test Subjective 3",

"objectiveNotes": "Test Objective 3",

"assessment": "Test Assessment 3",

"plans": "Test Plan 3"

}

## Screening Codes

API requirements: GET (read only)

### Get

Returns concept Ids with names for a practice screening codes. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

[**/getScreeningCodes?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13782258\_491**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

"patientId": "941286",

"resourceType": "ScreeningCodes",

"system": "hss",

"entry": \[

{

"conceptId": "60621009",

"screeningShortName": "BMI",

"screeningName": "Body Mass Index"

},

{

"conceptId": "276361009",

"screeningShortName": "WAIST",

"screeningName": "Waist Circumference"

},

{

"conceptId": "315038009",

"screeningShortName": "CVRA",

"screeningName": "CVRA%"

},

{

"conceptId": "129899009",

"screeningShortName": "BP",

"screeningName": "BP mmHg"

},

{

"conceptId": "50373000",

"screeningShortName": "HT",

"screeningName": "Height"

},

{

"conceptId": "27113001",

"screeningShortName": "WT",

"screeningName": "Weight Kg"

}

\]

}

## Encouter Summary

API requirements: POST

### Diabetes Project – POST

Code = DIAP

[/api/SaveSummary](https://staginghss.indici.nz/api/SaveSummary)

Writes an encounter summary data: Diabetes data for the patient

POST Request

{

     "patientId": "1950057",

     "encounterId": "28999606\_491",

    "resourceType": "Basic",

    "identifier": "DIAP",

    "entry": \[

        {

            "Diabetic risk":"Low",

            "HbA1c":"60",

            "Type of Diabetes":"Type 1",

            "Year of Diagnosis":"2018", 

            "Height":"164",

            "Weight":"84",

            "Year Last Retinal":"2015",

            "BP Systolic":"80",

            "BP Diastolic":"90",

            "Creatine Ratio":"2",

            "Dip-Stick Test":"Negative",

            "Total Cholesterol":"40",

            "HDL Cholesterol":"50",

            "Triglyceride":"20",

            "Ethnic Origin":"Maori",

            "NHI":"ZZZ0026",    

            "Smoker":"No"   ,

            "Insulin":"No"  ,   

            "Oral Medication":"No"  ,   

            "Diet Only":"No"    ,   

            "ACE Inhibitor":"No"    ,

            "Anti-hypertensive":"No"    ,

            "Statin inhibitor":"No" ,   

            "Other medication":"No" ,   

            "Foot Sensation/Pulse":"No" ,   

            "Prev PVD/Ulcer":"No"   ,   

            "Food Changes":"No" ,

            "Activity":"No",

            "OutCome":"A"           

            }

    \]

} 

Response

{

    "status": "success"

}

### Diabetic Foot Examination

[/api/SaveSummary](https://staginghss.indici.nz/api/SaveSummary)

Code : DS:FS

Writes an encounter summary data: Diabetic Foot Examination data for the patient

POST Request

{

     "patientId": "1950057",

     "encounterId": "28999606\_491",

    "resourceType": "Basic",

    "identifier": "DS:FS",

    "entry": \[

        {

            "Foot Risk:":"Low",

            "Date":"17/09/2012",

            "LOPS sites (/12 sites):":"2",

            "LOPS:":"No",

            "Painful Neuropathy:":"No",

            "Specify Neuropathy:":"Test123",

            "Rt Dorsalis Pedis:":"Yes",

            "Lt Dorsalis Pedis:":"Yes",

            "Rt Post. Tibial:":"Yes",

            "Lt Post. Tibial:":"Yes",

            "Previous Vascular:":"Yes",

            "Vascular When:":"Test",

            "Claudication:":"Yes",

            "Night Pain:":"Yes",

            "Vascular Describe:":"Test",

            "Amputation:":"No",

            "Prev. Ulceration:":"No",

            "Deformity:":"No",

            "End Stage Renal:":"No",

            "Callous/pre-ulcer:":"No",

            "Maori:":"No",

            "Can self Manage:":"No",

            "Other risk factors:":"Nothing",

            "Active Ulceration:":"No",

            "Suspected Charcot:":"No",

            "Recall:":"365"

         }

    \]

} 

Response

{

    "status": "success"

}

### Retinopathy

[/api/SaveSummary](https://staginghss.indici.nz/api/SaveSummary)

Code : DS:RET

Writes an encounter summary data: retinopathy data for the patient

POST Request

{

     "patientId": "1950057",

     "encounterId": "28999606\_491",

    "resourceType": "Basic",

    "identifier": "DS:RET",

    "entry": \[

        {

            "RET\_DATE": "29/10/2020", 

            "Right eye - Visual Acuity": "3/5",

            "Left eye - Visual Acuity": "6/5",

            "Retinopathy Status": "R8",

            "Macular status": "M18",

            "In Treatment": true,

            "Referral made": false,

            "Cataract present": false,

            "Refer for screening": "done"

            }

    \]

} 

Response

{

    "status": "success"

}

### Point of care test result

/api/SaveSummary

Code : WB:POCR

Writes an encounter summary data: Point of care Test data for the patient

POST Request

{

     "patientId": "1950057",

     "encounterId": "28999606\_491",

    "resourceType": "Basic",

    "identifier": " WB:POCR",

    "entry": \[

        {

> "Hb :":"11",
> 
> "wcc :":"11",
> 
> "Neuts :":"11",
> 
> "Creatinine :":"11",
> 
> "Sodium :":"11",
> 
> "Potassium :":"11",
> 
> "CRP :":"11",
> 
> "Glucose :":"11",
> 
> "NT Pro BNP :":"11",
> 
> "INR :":"11",
> 
> "Today :":"12/06/2021",
> 
> "POCT Device Name :" :"test",
> 
> "Batch No.":"1",
> 
> "Expiry Date :" :"12/06/2021"

}

    \]

} 

Response

{

    "status": "success"

}

### NZ Early warning score

/api/SaveSummary

Code : WB:NZEWS

Writes an encounter summary data: NZ Early warning score data for the patient

POST Request

{

     "patientId": "1950057",

     "encounterId": "28999606\_491",

    "resourceType": "Basic",

    "identifier": " WB:NZEWS",

    "entry": \[

        {

> "Resp Rate :":"11",
> 
> "SpO2 :":"11",
> 
> "On Oxygen :":"No",
> 
> "Temperature :":"11",
> 
> "BP Systolic :":"11",
> 
> "BP Diastolic :":"11",
> 
> "Heart Rate :":"11",
> 
> "Conscious Level :":"Alert"

}

    \]

} 

Response

{

    "status": "success"

}

### Oxygen Saturation

/api/SaveSummary

Code : SpO2

Writes an encounter summary data: Oxygen Saturation data for the patient

POST Request

{

"patientId":"1723176",

"encounterId":"29001505\_491",

"resourceType":"Basic",

    "identifier":"SpO2",

    "entry":\[{"Oxygen Saturation":"1",

"Heart Rate":"1"}\]}

Response

{

    "status": "success"

}

### PoC Outcomes

/api/SaveSummary

Code : PoCO

Writes an encounter summary data: POC Outcomes data for the patient

POST Request

{

"patientId":"1723176",

"encounterId":"29001505\_491",

"resourceType":"Basic",

    "identifier":"PoCO",

    "entry":\[{"Outline :":"2",

"Outcome No PoC :":"2",

"Outcome From PoC :":"2",

"Change of Management :":"2",

"Additional Comments :":"dd"}\]}

Response

{

    "status": "success"

}

### PoC Troponin 0 Hours

/api/SaveSummary

Code : PoCT0H

Writes an encounter summary data: POC Troponin 0 Hours data for the patient

POST Request

{

"patientId":"1723176",

"encounterId":"29001505\_491",

"resourceType":"Basic",

    "identifier":"PoCT0H",

    "entry":\[{"PoCT Device Name :":"test",

"Batch No.":"123",

"Expiry Date :":"02/06/2021",

"Troponin I 0 hours :":"1",

"Troponin I Units :":"1",

"Troponin Date :":"02/06/2021"}\]}

Response

{

    "status": "success"

}

### PoC Troponin 2 Hours

/api/SaveSummary

Code : PoCT2H

Writes an encounter summary data: POC Troponin 2 Hours data for the patient

POST Request

{

"patientId":"1723176",

"encounterId":"29001505\_491",

"resourceType":"Basic",

    "identifier":"PoCT2H",

    "entry":\[{"PoCT Device Name :":"test2",

"Batch No.":"1",

"Expiry Date :":"02/06/2021",

"Troponin I 2 hours :":"1",

"Troponin I Units :":"1",

"Troponin Date :":"02/06/2021"}\]}

Response

{

    "status": "success"

}

### ED Assessment of Chest Pain Score

/api/SaveSummary

Code : EDACS

Writes an encounter summary data: ED Assessment of chest pain data for the patient

POST Request

{

"patientId":"1723176",

"encounterId":"29001505\_491",

"resourceType":"Basic",

    "identifier":"EDACS",

    "entry":\[{"EDACS :":"111"}\]}

Response

{

    "status": "success"

}

## Lab Results

API requirements: GET (read only)

### Get

Returns current patient’s Lab results data. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

**/getLabResults?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13782258\_491**

{

"patientId": "941286",

"resourceType": "Labs",

"system": "hss",

"entry": \[

{

"messageSubject": "HBA1c",

"title": "HbA1c (IFCC)",

"code": "pathlabs|0063|",

"effectiveDateTime": "2017-07-29T01:54:42",

"value": "34"

},

**…\[SNIP\]…**

{

"messageSubject": "LIPIDS..........",

"title": "Cholesterol",

"code": "pathlabs|1080|",

"effectiveDateTime": "2017-07-29T01:54:22",

"value": "6.8"

},

{

"messageSubject": "LIPIDS..........",

"title": "HDL Cholesterol",

"code": "pathlabs|1080|",

"effectiveDateTime": "2017-07-29T01:54:22",

"value": "1.97"

},

{

"messageSubject": "LIPIDS..........",

"title": "LDL Chol - calculated",

"code": "pathlabs|1080|",

"effectiveDateTime": "2017-07-29T01:54:22",

"value": "4.2"

},

{

"messageSubject": "LIPIDS..........",

"title": "Lipid Comments.",

"code": "pathlabs|1080|",

"effectiveDateTime": "2017-07-29T01:54:22",

"value": "A combined CVD risk, of which lipids is one component, should be estimated to guide CVD risk management decisions. If lipid modifying medication is considered, suggest checking first for treatable secondary causes of dyslipidaemia."

},

**…\[SNIP\]…**

{

"messageSubject": "TFTs............",

"title": "TSH",

"code": "pathlabs|1540|",

"effectiveDateTime": "2017-07-29T01:54:22",

"value": "4.42"

}

\]

}

## Inbox Documents

API requirements: GET (read only)

### Get - Collection

Returns current patient’s Inbox (non-Lab/Rad) documents data. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

**/getDocuments?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13782258\_491**

{

"patientId": "941286",

"resourceType": "Documents",

"system": "hss",

"entry": \[

{

"createdDateTime": "2018-02-12T14:02:53",

"messageSubject": "Ezetimibe patient information leaflet",

"identifier": "83553fb4-8d7f-4e74-b9b1-a97b38f88939",

"messageTitle": null,

"messageData": null

},

{

"createdDateTime": "2018-07-04T12:16:34",

"messageSubject": "PHQ 9",

"identifier": "c98a9860-5c95-4862-ab47-f773afc6f938",

"messageTitle": null,

"messageData": null

}

\]

}

### Get – Single Document

Returns document’s base64 data against a provided identifier. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

**/getDocuments?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13782258\_491\&identifier= 83553fb4-8d7f-4e74-b9b1-a97b38f88939**

{

"patientId": "941286",

"resourceType": "Documents",

"system": "hss",

"entry": \[

{

"createdDateTime": "2018-02-12T14:02:53",

"messageSubject": null,

"identifier": null,

"messageTitle": "Ezetimibe patient information 12-Feb-18.pdf",

"messageData": "JVBERi0xLjY...\[SNIP\]...YNCiUlRU9GDQo="

}

\]

}

### Post –Document

Post document’s base64 data for a patient. Successful save will return a success message (JSON) along with the Guid identifier of that particular document and a failure will return the error.

**/saveDocument**

{

"patientId": "941286",

"encounterId": "13782258",

"userId": "",

"resourceType":"Document",

"system": "hss",

"messageTitle": "Diabetes Review",

"messageSubject": "Diabetes Review",

"contentType": "text/html",

"itemType": "out",

"messageData": "JVBERi0x...\[SNIP\]...pemUgNTE+Pg0Kc3RhcnR4cmVmDQoxMTYNCiUlRU9GDQo="

}

<span class="underline">SUCCESS RESPONSE:</span>

{

"status": "success",

"message": "83553fb4-8d7f-4e74-b9b1-a97b38f88939"

}

<span class="underline">FAIL RESPONSE:</span>

{

"status": "fail",

"message": "Authentication failed\!"

}

## Observations

API requirements: GET (read only)

### Get

Returns current patient’s observed/observations/screening data. This can include the following listing with SnomedCT Ids:

| S. No. | Concept ID | Term        |
| ------ | ---------- | ----------- |
| 1      | 27113001   | Weight      |
| 2      | 50373000   | Height      |
| 3      | 60621009   | BMI         |
| 4      | 129899009  | BP          |
| 5      | 276361009  | Waist Circ. |
| 6      | 315290008  | CVD Risk    |
| 7      |            | Temperature |
| 8      |            | Heart Rate  |

Parameter conceptId is to entertain a single listing when a valid conceptId is provided. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

<span class="underline">w/o concpetId</span>

**/getObservations?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13783775**

{

"patientId": "941286",

"resourceType": "Screening",

"system": "hss",

"entry": \[

{

"observationDate": "2018-10-10T18:13:40",

"conceptId": "129899009",

"shortName": "BP Sys",

"name": "BP Systolic",

"value": "120",

"units": ""

},

{

"observationDate": "2018-10-10T18:13:40",

"conceptId": "129899009",

"shortName": "BP Dia",

"name": "BP Diastolic",

"value": "80",

"units": ""

},

{

"observationDate": "2018-10-10T18:01:04",

"conceptId": "27113001",

"shortName": "WT",

"name": "Weight",

"value": "70",

"units": "Kg."

},

{

"observationDate": "2018-10-10T18:14:23",

"conceptId": "276361009",

"shortName": "WC",

"name": "Waist Circumference",

"value": "100",

"units": "cm"

},

**…\[SNIP\]…**

{

"observationDate": "2018-10-10T18:02:56",

"conceptId": "60621009",

"shortName": "BMI",

"name": "Body Mass Index",

"value": "21.6",

"units": ""

}

\]

}

<span class="underline">w/ concpetId</span>

**/getObservations?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13783775\&conceptId=27113001**

{

"patientId": "941286",

"resourceType": "Screening",

"system": "hss",

"entry": \[

{

"observationDate": "2018-10-10T18:01:04",

"conceptId": "27113001",

"shortName": "WT",

"name": "Weight",

"value": "70",

"units": "Kg."

}

\]

}

### Post

To post a JSON to save observations against the current patient. Successful save will return a success message (JSON) and a failure will return the error.

| Units       |            |
| ----------- | ---------- |
| Temperature | Centigrade |
| Height      | Centimetre |
| Weight      | Kilogram   |
| Heart Rate  | BPM        |

[**/saveObservations**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

"patientId": "941286",

"encounterId": "13783775",

"userId": "",

"resourceType":"Observations",

"system": "hss",

"temperature": "",

"waistcircumference": "",

"height": "",

"weight": "",

"bpsys": "",

"bpdia":"",

"heartrate":"",

"risk":"\>15",

"framingham":"5",

"notes":"Test notes"

}

## Conditions/Classifications

API requirements: GET (read only)

### Get

Returns current patient’s diagnosis data. This also is termed classifications or conditions. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

**/getConditions?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13783775**

{

"patientId": "941286",

"resourceType": "Conditions",

"system": "hss",

"entry": \[

{

"diagnosisDate": "2018-10-12",

"conceptId": "386661006",

"name": "Fever ",

"fsn": "Fever (finding)",

"type": "Clinical Findings",

"onSetDate": "2018-10-10",

"summary": "",

"isLongTerm": "false"

},

{

"diagnosisDate": "2018-10-19",

"conceptId": "13645005",

"name": "Chronic obstructive lung disease",

"fsn": "Chronic obstructive lung disease (disorder)",

"type": "Clinical Findings",

"onSetDate": "2000-01-01",

"summary": "",

"isLongTerm": "false"

},

**\[…SNIP…\]**

{

"diagnosisDate": "2019-01-29",

"conceptId": "416720006",

"name": "Hepatic pump ",

"fsn": "Hepatic pump (procedure)",

"type": "Clinical Findings",

"onSetDate": "",

"summary": "Liver Testing Data",

"isLongTerm": "true"

}

\]

}

### Post

To post a JSON to save a classification/condition against the current patient’s encounter.

The concept Id (SNOMED) should match with the name of the condition. Fully Specified Name (FSN) for the condition is optional.

Successful save will return a success message (JSON) and a failure will return the error.

| Valid Types |                   |
| ----------- | ----------------- |
|             | Clinical Findings |
|             | Event             |
|             | Family History    |
|             | Observable entity |
|             | Procedures        |
|             | Regime/therapy    |
|             | Situation         |
|             | Social Concept    |
|             | Special Concept   |

[**/saveCondition**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

"patientId": "941286",

"encounterId": "13783795",

"userId": "",

"resourceType":"Condition",

"system": "hss",

"type": "Clinical Findings",

"conceptId": "18165001",

"name": "Jaundice",

"fsn":" Jaundice (finding)",

"onSetDate": "2019-02-01T00:00:00",

"isLongTerm": "false",

"summary":"Test summary"

}

## Recalls

API requirements: GET (read only)

### Get

Returns current patient’s recall data. Where no return data is available, either null or "" is used. Both must be checked for and treated equivalently by the consuming application. In numeric data field expect a 0 equivalent to null.

**/getRecalls?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13783775**

{

"patientId": "941286",

"resourceType": "Recalls",

"system": "hss",

"entry": \[

{

"group": "Vaccine Schedule",

"category": "3M 1980",

"priority": "Medium",

"dueDate": "1980-04-01",

"notes": "",

"reason": ""

},

{

"group": "Vaccine Schedule",

"category": "5M 1980",

"priority": "Medium",

"dueDate": "1980-06-01",

"notes": "",

"reason": ""

},

**…\[SNIP\]…**

{

"group": "Vaccine Schedule",

"category": "11Y 1980",

"priority": "Medium",

"dueDate": "1991-01-01",

"notes": "",

"reason": ""

}

\]

}

### Post

To post a JSON to save recall against the current patient’s encounter.

If the property “*categoryId*” is left empty, the system will pick the first for the group selected.

In order to explicitly supply a valid *categoryId*, an API call to the Recall Categories needs to be made first. Please see the Recall Categories operation.

Successful save will return a success message (JSON) and a failure will return the error.

| Valid Group |                  |
| ----------- | ---------------- |
|             | Bloods           |
|             | Medicine         |
|             | Miscellaneous    |
|             | Procedure        |
|             | Referral         |
|             | Screening        |
|             | Service Template |
|             | Vaccine          |
|             | Vaccine Group    |
|             | Vaccine Schedule |

| Valid Priority |        |
| -------------- | ------ |
|                | Low    |
|                | Medium |
|                | High   |

[**/saveRecall**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

"patientId": "941286",

"encounterId": "13783775",

"userId": "",

"resourceType":"Recall",

"system": "hss",

"group": "Medicine",

"categoryId": "",

"priority": "High",

"dueDate": "2019-12-01T15:00:00",

"notes": "Random test notes"

}

## Recall Categories

API requirements: GET (read only)

### Get

Returns list of Recall categories for a given Recall group. This is to facilitate the operation to POST a recall into indici system.

**/getRecallCategories?system=hss\&pho=NBPH\&patientId=941286\&encounterId=13782258\_491\&group=medicine**

{

"patientId": "941286",

"resourceType": "RecallCategories",

"system": "hss",

"entry": \[

{

"id": "154034",

"name": "13 mm angle cannula with 10 needles",

"code": ""

},

{

"id": "38",

"name": "3.4-Diaminopyridine (Link) (amifampridine (as phosphate) 10 mg) tablet: uncoated, 1 tablet",

"code": ""

},

**…\[SNIP\]…**

{

"id": "155998",

"name": "Zostavax (Varicella zoster virus (Oka strain) live",

"code": ""

},

{

"id": "965",

"name": "Zytiga (abiraterone acetate 250 mg) tablet: uncoated, 1 tablet",

"code": ""

}

\]

}

## Invoice

### Post

To post a JSON to save invoice/claim against the current patient’s encounter.

[**/saveInvoice**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

{

> "patientId":"1950057",
> 
> "encounterId":"28999735\_491",
> 
> "resourceType":"InvoiceClaim",
> 
> "system":"hss",
> 
> "userId":"1463397",
> 
> "locationId":"",
> 
> "name":"COVID-19 Assessment test 123456",
> 
> "code":"COVIDS",
> 
> "claimType":"COVID",
> 
> "fee":"18.00",
> 
> "payee":"222001"

}
