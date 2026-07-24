![C:\\Users\\abdullah.noor\\Downloads\\Vaentia-Logo700X245.png](media/image1.png)

Indici *ERMS* Web API Specification

| Created Date | 19/11/2018 |
| ------------ | ---------- |
| Updated Date | 05/03/2019 |
| Author       | Abdullah   |
| Version      | 1.1.2      |

**  
**

# Table of Contents

[1. Document Details 5](#document-details)

[1.1 Version 5](#version)

[2. Purpose 8](#purpose)

[3. Overview 8](#overview)

[3.1 indici - Electronic Health Record (EHR) 8](#indici---electronic-health-record-ehr)

[3.2 ERMS – Electronic Request Management System 9](#erms-electronic-request-management-system)

[4. Authentication 9](#authentication)

[4.1 Patient ID AND Encounter ID (and Other parameters) 9](#patient-id-and-encounter-id-and-other-parameters)

[4.1.1 Patient ID 9](#patient-id)

[4.1.2 Encounter ID 10](#encounter-id)

[4.1.3 User ID 10](#user-id)

[4.1.4 Practice ID 10](#practice-id)

[4.1.5 Location ID 10](#location-id)

[4.2 Invocation 10](#invocation)

[4.2.1 ERMS Access Through indici 10](#erms-access-through-indici)

[4.2.2 Indici ERMS Web API/Portal 11](#indici-erms-web-apiportal)

[4.2.3 Indici PMS System Access 11](#indici-pms-system-access)

[4.2.4 Sample Code (Javascript) 12](#sample-code-javascript)

[5. Data Requests 12](#data-requests)

[5.1 Ping 12](#ping)

[5.1.1 Get 12](#get)

[5.2 Authenticate 12](#authenticate)

[5.2.1 Post 13](#post)

[5.3 Current User 13](#current-user)

[5.3.1 Get 13](#get-1)

[5.4 Patient Data 14](#patient-data)

[5.4.1 Get 14](#get-2)

[5.5 Problems/Classifications 15](#problemsclassifications)

[5.5.1 Get 15](#get-3)

[5.6 Regular Medications 17](#regular-medications)

[5.6.1 Get 17](#get-4)

[5.7 Prescribed Medications 17](#prescribed-medications)

[5.7.1 Get 18](#get-5)

[5.8 Consult Notes 19](#consult-notes)

[5.8.1 Get 19](#get-6)

[5.9 Next of Kin 20](#next-of-kin)

[5.9.1 Get 20](#get-7)

[5.10 Allergies/Warnings 21](#allergieswarnings)

[5.10.1 Get 21](#get-8)

[5.11 Registered Practitioners 21](#registered-practitioners)

[5.11.1 Get 21](#get-9)

[5.12 Smoking Status 24](#smoking-status)

[5.12.1 Get 24](#get-10)

[5.13 Accidents 24](#accidents)

[5.13.1 Get 25](#get-11)

[5.14 Measurements 25](#measurements)

[5.14.1 Get 25](#get-12)

[5.15 Lab Reports Listing 26](#lab-reports-listing)

[5.15.1 Get 26](#get-13)

[5.16 Lab Report Details 27](#lab-report-details)

[5.16.1 Get 27](#get-14)

[5.17 Radiology Reports Listing 27](#radiology-reports-listing)

[5.17.1 Get 27](#get-15)

[5.18 Radiology Reports Details 28](#radiology-reports-details)

[5.18.1 Get 28](#get-16)

[5.19 Discharge Summary Listing 28](#discharge-summary-listing)

[5.19.1 Get 29](#get-17)

[5.20 Discharge Summary Details 29](#discharge-summary-details)

[5.20.1 Get 29](#get-18)

[5.21 Save/Upload Document 30](#saveupload-document)

[5.21.1 POST 30](#post-1)

[5.22 Scanned Document Listing 31](#scanned-document-listing)

[5.22.1 Get 31](#get-19)

[5.23 Scanned Document Details 32](#scanned-document-details)

[5.23.1 Get 32](#get-20)

# Document Details

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
<td>19/11/2018</td>
<td>1.0.0</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>First draft created.</p></li>
</ul></td>
</tr>
<tr class="even">
<td>23/11/2018</td>
<td>1.0.1</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Added details for 2 operations</p></li>
<li><p>Updated Authentication</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>30/11/2018</td>
<td>1.0.2</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Added 4 new methods/operations: Classifications, Medications (Regular/Prescribed) and Consult Notes</p></li>
<li><p>Renamed title Demographic to Patient Data</p></li>
</ul></td>
</tr>
<tr class="even">
<td>05/12/2018</td>
<td>1.0.3</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Added 2 new methods: Next Of Kin, Medical Allergies/Warnings</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>12/12/2018</td>
<td>1.0.4</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Added method: Registered Practitioners.</p></li>
<li><p>Updated response XMLs with new data</p></li>
<li><p>Updated method Authenticate response (Time zone addition)</p></li>
</ul></td>
</tr>
<tr class="even">
<td>14/12/2018</td>
<td>1.0.5</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Added 2 methods/concepts: Smoking Status, Accidents</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>20/12/2018</td>
<td>1.0.6</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Parameter PracticeId in ERMS invocation URI</p></li>
<li><p>Street number introduced in Patient Data/NoK</p></li>
<li><p>Updated InternalPMSID to guid</p></li>
<li><p>Date format correction in Classification/Medications/Allergies/Notes</p></li>
<li><p>Read code in Classification</p></li>
<li><p>Multiple NoK entries</p></li>
<li><p>Added concepts: Measurements, Lab Reports listing, Lab Report Details</p></li>
</ul></td>
</tr>
<tr class="even">
<td>16/01/2019</td>
<td>1.0.7</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Added four concepts: Rad Report listing, Rad details, Discharge Summary listing, Discharge Summary details</p></li>
<li><p>Updated Invocation URLs</p></li>
<li><p>Added UserId parameter (to cater Current User) in Invocation URL</p></li>
<li><p>Correction to Current User concept: Handled User Id supplied by Invocation URL to retrieve current logged in user data.</p></li>
<li><p>Updated response for Current User concept</p></li>
<li><p>New 4.1.3, 4.1.4 sections to explain User Id, Practice Id</p></li>
<li><p>Added list of users for indici access – section 4.2.3</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>18/01/2019</td>
<td>1.0.8</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>QA/UAT Icons addition</p></li>
<li><p>New user ermsdev1 addition (making the total to ten users)</p></li>
<li><p>Correction: Work and Residence phone swap in Patient Data</p></li>
<li><p>Correction: Gender length issue throwing truncated error message in Patient Data</p></li>
<li><p>Correction: HUC/CSC data swap in Patient Data</p></li>
<li><p>Correction: Addresses for referrer in Registered Practitioners concept</p></li>
<li><p>Correction: Incomplete entries in Registered Practitioners concept</p></li>
<li><p>Correction: NZNC/NZMC difference in Registered Practitioners and Current User concept. Updated response XML for Registered Practitioners to reflect the difference.</p></li>
<li><p>Addition of Eligibility logic for Non NZ Residents in Patient Data concept</p></li>
</ul></td>
</tr>
<tr class="even">
<td>28/01/2019</td>
<td>1.0.9</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Correction of Level 2 ethnicity code in Patient Details concept</p></li>
<li><p>Correction for date against Medical Warning concept</p></li>
<li><p>Change to Comments and Description (split) for Medical Warning concept</p></li>
<li><p>Rename of params in all URL calls to have prefix “<em>pms</em>”. See section <strong>4.1</strong></p></li>
<li><p>Correction to Next of Kin concept to include missing Postcode</p></li>
<li><p>Correction of returned ACC date in Accident concept</p></li>
<li><p>Additional columns Assessment, Plan in Consult Notes concept</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>29/01/2019</td>
<td>1.1.0</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Renamed <em>RadiologyReport_DateCreated</em> to <em>RadiologyReport_DateReceived</em></p></li>
<li><p>Rights given to view Examinations</p></li>
<li><p>Corrected Read code for Smoking status</p></li>
<li><p>Removed medications with Stop status from Medications concepts</p></li>
<li><p>Changed street name to a value that shows unit/house number appended with street number. Concept: Practitioners Listing</p></li>
<li><p>New concept: SaveDocument</p></li>
<li><p>Correction: Date format correction in Authenticate call</p></li>
</ul></td>
</tr>
<tr class="even">
<td>18/02/2019</td>
<td>1.1.1</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Introduced locationId (pmsLocId) parameter to id the practice location within concept call Registered Practitioners</p></li>
<li><p>Updates sample response xml for concept Registered Practitioners</p></li>
<li><p>New concepts: GetScannedList, GetScannedDetails</p></li>
</ul></td>
</tr>
<tr class="odd">
<td>05/03/2019</td>
<td>1.1.2</td>
<td>Abdullah</td>
<td>Valentia Technologies</td>
<td><ul>
<li><p>Replaced ReferralDocument_Provider_Name with ReferralDocument_Referrer_PMS_ID in concept SaveDocument. This will have the pmsUserId value from the initial invocation call</p></li>
<li><p>Renamed pmsLocId to pmsLocationId on request</p></li>
<li><p>Updated DateTime for all dates in: GetPatientData, GetClassifications, GetRegularMedications, GetPrescribedMedications, GetMedicalAllergies</p></li>
</ul></td>
</tr>
</tbody>
</table>

# Purpose

The purpose of this document is to give an overview of the Web API that defines the data exchanged between the *indici Practice Management System* and *Electronic Request Management System (ERMS)*. It lists the operations exposed to be consumed by each side. The document also explain the authentication process through which ERMS will be able to gain authorized access to the Web API.

# Overview

This document specifies all the data required to be read and/or written back from indici Practice Management System. Since ERMS has chosen to handle this type of data interactions using a national standard called *HISO Concepts* and the use of *XML format*, the Web API will provide responses in such format. The Web API will handle the requests and parse from ERMS in this chosen format as well. The HISO concept codes shared by ERMS will identify each property in the data mapping model for both parties to process. It is noted that HISO concepts standard have been widely accepted in most part of New Zealand Healthcare industry and provides a pragmatic way of dealing with various HealthCare vendors with differing metadata. Currently, *HealthLink* manages the HISO concept codes in conjunction with other PMS vendors.

## indici - Electronic Health Record (EHR)

***indici*** is a web-based integrated Electronic Health Record (EHR), Practice Management and Billing Management solution.

Built in close collaboration with clinicians, allied health professionals, practice managers and patients, indici’s intuitive design, configurable workflows and state-of-the-art functionality address the evolving needs of all stakeholders.

indici’s EHR is a 360˚ Longitudinal Health Record, providing practitioners with a valuable, single source of information comprising each patient’s contact with healthcare across their lifetime.

Reflecting the rapidly evolving nature of out-of-hospital healthcare, indici supports secure multi-disciplinary Shared Care Planning and collaboration, enabling care to be pro-actively managed and coordinated, both within practices and at a wider healthcare level. As indici is web-based, Mobile Working is readily supported and information can be accessed and updated in any care setting at any time.

indici’s Practice Management functionality streamlines and automates the operational aspects of practices, including patient interaction, resource management and scheduling, appointments management and communications, in addition to providing real-time clinical and operational data and analytical capabilities.

## ERMS – Electronic Request Management System

ERMS is the South Island wide solution for creating and delivering electronic referrals to service providers. It is a full end to end solution which is integrated with both practice management systems and hospital clinical portals. Essentially, users can create a referral, send it to the most appropriate service provider, where it can be triaged electronically and a triage outcome message provided to the user.

ERMS is the endorsed Southern Regional solution which is currently in use across all five DHBs within the South Island. The eReferrals Programme is aligned with National IT directives and strategies and held accountable by the South Island Alliance.

# Authentication

Each API call for data retrieval and post requires authentication. The guiding principle is that an authentication string needs to be sent with each API call. This authentication string is the token initially generated in the first “hand-shake” call to the Web API. The string is a GUID value generated by indici ERMS Web API and is passed with the headers of the request call.

For it to start, the first call to the API should be to authenticate that needs to POST a formatted xml payload. The payload includes user name and password. Details of the calls are given in latter part of the document.

This authorization token obtained after successful authentication will be used in all the subsequent function calls of the API. The delivery of the token will ideally be Authorization property of http header.

The token has a set expiry which is included in the Authenticate API call response.

## Patient ID AND Encounter ID (and Other parameters)

<span class="underline">Note</span>: Each parameter appended in the URL must have a prefix “pms”. See URL examples below.

### Patient ID

Each vendor will provide a patient identifier that is unique across their system. In this case, indici will provide the ID in the invocation call as a parameter and the result query parameter needs to be parsed by *ERMS* and sent to the Web API with the call for authentication. This ID identifies the patient for indici system. NHI is not satisfactory for this purpose because not every patient that is seen will have an NHI.

### Encounter ID

The encounter ID is a mechanism that allows the Indici PMS to associate data with a particular consultation and provider.

An encounter ID string is included in each API call if required. If not required then it would be omitted from the query string or post payload. The initial call should have the value passed to *ERMS* portal for it to pass in the subsequent calls to the Web API.

It is up to the PMS to maintain the appropriate connection between encounter ID and patient/provider. The data associated with an encounter ID should be the provider who launched the portal using the designated button/link.

### User ID

The User Id is supplied to the invocation URL to ERMS page. This is the Identifier that uniquely identifies the current logged in user/practitioner. This information is supplied to the Current User concept for further action.

### Practice ID

The practice Id supplied to invocation URL indicates the user current practice.

### Location ID

The practice location Id supplied to invocation URL indicates the user current practice selected location.

## Invocation

### ERMS Access Through indici

Browser-based indici will enable a function to open ERMS portal through a designated link or button. The called URI will include the patient relevant information, for example *PatientId* and *EncouterId* as query string parameters. The identifiers can be extended and more can be introduced on request and requirement.

An example invocation URI used will consist of

1.  Host:
    
    1.  Development: [**http://ermsdeveloper/referralforms?pms=Indici**](http://ermsdeveloper/referralforms?pms=Indici)
    
    2.  QA: [**https://erms-q.srphc.health.nz/referralforms?pms=Indici**](https://erms-q.srphc.health.nz/referralforms?pms=Indici)
    
    3.  UAT: [**https://erms-u.srphc.health.nz/referralforms?pms=Indici**](https://erms-u.srphc.health.nz/referralforms?pms=Indici)
    
    4.  Production: **<https://erms.srphc.health.nz/referralforms?pms=Indici>**
    
    5.  Production offline: [**https://erms.srphc.health.nz/referralforms\_offline?pms=Indici**](https://erms.srphc.health.nz/referralforms_offline?pms=Indici)

2.  Query strings:
    
    1.  Patient ID: **\&pmsPatientId=941819**
    
    2.  Encounter ID: **\&pmsEncounterId=13780398**
    
    3.  Practice ID: **\&pmsPracticeId=6**
    
    4.  User ID: **\&pmsUserId= 941823**
    
    5.  Location ID: **\&pmsLocationId=11**

<!-- end list -->

1.  
2.  
### 

### Indici ERMS Web API/Portal

The Web API URL used will consist of

1.  Host:
    
    1.  Development: **[https://deverms.itsmyhealth.nz/api/{Operation-Name](https://deverms.itsmyhealth.nz/api/%7bOperation-Name)}**
    
    2.  Production: **[https://\[Valentia-Production-Web-API\]/api/{Operation-Name](https://[Valentia-Production-Web-API]/api/%7bOperation-Name)}**

ERMS is described to be integrated with the PMS and launched in a browser window by clicking the ERMS icon/link. When ERMS is launched, it is expected ERMS opens on the select referral screen and has the patient in content details populated on the ERMS banner.

### Indici PMS System Access

The Web portal for Indici PMS (limited) can be accessed:

URL: **<https://pmstraining.itsmyhealth.nz>:444**

<span class="underline">Username</span>: ermsdev  
<span class="underline">Password</span>: \*\*\*

<span class="underline">Additional Users</span>: *ermsdev1*, *ermsdev2, ermsdev3, ermsdev4, ermsdev5, ermsdev6, ermsdev7, ermsdev8, ermsdev9* (all with same above password)

**Patient**: Patient ERMS (you click on the name of the patient)

<span class="underline">Note</span>: The Patient Consult page opens in a new popup/window. Make sure you have disabled your popup-blocker for the site.

<span class="underline">ERMS Icons (Dev/QA/UAT):</span>

![](media/image2.png)

### Sample Code (Javascript)

function CallWebAPI() {

var xhttp = new XMLHttpRequest();

xhttp.onreadystatechange = function() {

if (this.readyState == 4 && this.status == 200) {

//your parsing code

}};

xhttp.open("GET", " https://deverms.itsmyhealth.nz/api/ping", true);

xhttp.setRequestHeader("Content-type", "text/xml");

//xhttp.setRequestHeader("Authorization", "\[Auth Token\]");

xhttp.send();

}

# Data Requests

## Ping

API requirements: GET (read only)

### Get

Returns status (up) of the API.

[**/ping**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

\<?xml version="1.0" encoding="utf-16"?\>  
\<Ping\>Success\!\</Ping\>

## Authenticate

API requirements: POST

<span class="underline">Properties</span>:

1.  username = *ermsdev*

2.  password = *\*\*\**

3.  patientId = 941819

4.  encounterId = 13780398

### Post

Returns a generated token when correct credentials are provided (string). The token is used for authentication in the subsequent calls

**/authenticate**

\<?xml version="1.0" encoding="utf-16"?\>

\<Credential\>

\<Username\>**ermsdev**\</Username\>

\<Password\>**eR\*\*\*\*\***\</Password\>

\<PatientId\>**941819**\</PatientId\>

\<EncounterId\>**13780398**\</EncounterId\>

\</Credential\>

<span class="underline">SUCCESS RESPONSE:</span>

\<?xml version="1.0" encoding="utf-16"?\>

\<Authentication\>

\<Token\>**dc3a3117-3e59-46ab-9dc0-8d994b6f1d68**\</Token\>

\<Expiry\>**2018-07-13T10:07:55+13**\</Expiry\>

\<PracticeId\>**demo**\</PracticeId\>

\</Authentication\>

<span class="underline">FAIL RESPONSE:</span>

\<?xml version="1.0" encoding="utf-16"?\>

\<Error\>

\<Message\>**Authentication failed\!**\</Message\>

\</Error\>

## Current User

API requirements: GET (read only)

Additional Parameters:

1.  User Id – provided in the invocation URL (see section **4.2.1**)

### Get

Returns current patient’s provider data. That is, the person logged in the PMS and creating the referral.

**/getCurrentUser?pmsPatientId=941819\&pmsEncounterId=13781624\&pmsUserId=941823**

\<?xml version="1.0" encoding="utf-16"?\>

\<CurrentUser xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"\>

\<CurrentUser\_FirstName conceptID="554:1000123/1000310/1"\>**Erms**\</CurrentUser\_FirstName\>

\<CurrentUser\_Surname conceptID="554:1000123/1000310/2"\>**Admin**\</CurrentUser\_Surname\>

\<CurrentUser\_Middlename conceptID="554:1000123/1000310/3" /\>

\<CurrentUser\_FullName conceptID="554:1000123/1000310/7"\>**Erms ADMIN**\</CurrentUser\_FullName\>

\<CurrentUser\_RegisteringBody conceptID="554:1000123/1000421"\>**NZMC**\</CurrentUser\_RegisteringBody\>

\<CurrentUser\_RegistrationNumber conceptID="554:1000123/1000420"\>**72796**\</CurrentUser\_RegistrationNumber\>

\<CurrentUser\_PersonalHPI conceptID="554:1000123/1000410"\>**HPI002**\</CurrentUser\_PersonalHPI\>

\<CurrentUser\_Application\_UserID conceptID="554:1000123/1000310/8"\>**EE238491-EEF6-4182-BA3C-6B00550F2FB6**\</CurrentUser\_Application\_UserID\>

\<CurrentUserOrganisation\_FacilityHPI conceptID="554:1000123/1000745"\>**F2M067**\</CurrentUserOrganisation\_FacilityHPI\>

\<CurrentUserOrganisation\_HealthlinkEDI conceptID="554:1000123/1000409"\>**grand2vw**\</CurrentUserOrganisation\_HealthlinkEDI\>

\<CurrentUser\_PMSID conceptID="554:1000123/1000310/9"\>**ermsdev**\</CurrentUser\_PMSID\>

\</CurrentUser\>

## Patient Data

API requirements: GET (read only)

### Get

Returns current demographic data for the patient. This include patient data such as address, contact, and ethnicity.

[**/getPatientData?pmsPatientId=941819\&pmsEncounterId=13780398**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

\<?xml version="1.0" encoding="utf-16"?\>

\<PatientData xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"\>

\<Patient\_Surname conceptID="554:1000111/1000310/2"\>**PATIENT**\</Patient\_Surname\>

\<Patient\_FirstName conceptID="554:1000111/1000310/1"\>**Erms**\</Patient\_FirstName\>

\<Patient\_MiddleName conceptID="554:1000111/1000310/3"\>**Patient**\</Patient\_MiddleName\>

\<Patient\_NHI conceptID="554:1000111/1000360"\>**ZCN4440**\</Patient\_NHI\>

\<Patient\_DateOfBirth conceptID="554:1000111/1000320"\>**1985-05-05T00:00:00**\</Patient\_DateOfBirth\>

\<Patient\_Gender conceptID="554:1000111/1000350"\>**F**\</Patient\_Gender\>

\<Patient\_ResidentialAddress\_StreetNumber conceptID="554:1000111/1000340/2"\>**133**\</Patient\_ResidentialAddress\_StreetNumber\>

\<Patient\_ResidentialAddress\_StreetName conceptID="554:1000111/1000340/3"\>**Molesworth Street** \</Patient\_ResidentialAddress\_StreetName\>

\<Patient\_ResidentialAddress\_Suburb conceptID="554:1000111/1000340/5"\>**Thorndon**\</Patient\_ResidentialAddress\_Suburb\>

\<Patient\_ResidentialAddress\_City conceptID="554:1000111/1000340/7"\>**Wellington**\</Patient\_ResidentialAddress\_City\>

\<Patient\_ResidentialAddress\_Postcode conceptID="554:1000111/1000340/6"\>**6011**\</Patient\_ResidentialAddress\_Postcode\>

\<Patient\_ResidentialAddress\_AdditionalLine conceptID="554:1000111/1000340/4" /\>

\<Patient\_PostalAddress\_StreetNumber conceptID="554:1000111/1000400/2"\>**10A**\</Patient\_PostalAddress\_StreetNumber\>

\<Patient\_PostalAddress\_StreetName conceptID="554:1000111/1000400/3"\>**Little London Lane**\</Patient\_PostalAddress\_StreetName\>

\<Patient\_PostalAddress\_Suburb conceptID="554:1000111/1000400/5"\>**Hamilton Central**\</Patient\_PostalAddress\_Suburb\>

\<Patient\_PostalAddress\_City conceptID="554:1000111/1000400/7"\>**Hamilton**\</Patient\_PostalAddress\_City\>

\<Patient\_PostalAddress\_Postcode conceptID="554:1000111/1000400/6"\>**3204**\</Patient\_PostalAddress\_Postcode\>

\<Patient\_PostalAddress\_AdditionalLine conceptID="554:1000111/1000400/4" /\>

\<Patient\_Ethnicity1CodeLevel2 conceptID="554:1000111/1000370/1"\>**12943**\</Patient\_Ethnicity1CodeLevel2\>

\<Patient\_Ethnicity2CodeLevel2 conceptID="554:1000111/1000370/2"\>**21**\</Patient\_Ethnicity2CodeLevel2\>

\<Patient\_Ethnicity3CodeLevel2 conceptID="554:1000111/1000370/3"\>**11**\</Patient\_Ethnicity3CodeLevel2\>

\<Patient\_Email conceptID="554:1000111/1000390"\>**ERMS@pegasus.co.nz**\</Patient\_Email\>

\<Patient\_Mobile conceptID="554:1000111/1000331"\>**+6421111111**\</Patient\_Mobile\>

\<Patient\_ResidentialPhone conceptID="554:1000111/1000330"\>**+6421333333**\</Patient\_ResidentialPhone\>

\<Patient\_WorkPhone conceptID="554:1000111/1000332"\>**+6421222222**\</Patient\_WorkPhone\>

\<Patient\_IsEligiblePublicFunds conceptID="554:1000111/1000382"\>**T**\</Patient\_IsEligiblePublicFunds\>

\<Patient\_HUC conceptID="554:1000111/1000391"\>**7894561564132**\</Patient\_HUC\>

\<Patient\_HUC\_StartDate conceptID="554:1000111/1000392" /\>

\<Patient\_HUC\_EndDate conceptID="554:1000111/1000393"\>**2020-02-01T00:00:00**\</Patient\_HUC\_EndDate\>

\<Patient\_CSC conceptID="554:1000111/1000395"\>0000068698312003\</Patient\_CSC\>

\<Patient\_CSC\_StartDate conceptID="554:1000111/1000396" /\>

\<Patient\_CSC\_EndDate conceptID="554:1000111/1000397"\>**2019-11-09T00:00:00**\</Patient\_CSC\_EndDate\>

\<Patient\_InternalPMSID conceptID="554:1000111/1000399"\>**6A22DBBA-F3A1-4511-857A-6FEDBF6236A5**\</Patient\_InternalPMSID\>

\</PatientData\>

## Problems/Classifications

API requirements: GET (read only)

Additional Parameters:

1.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

2.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

3.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns diagnosis/classification/problem details for the patient.

[**/getClassifications?pmsPatientId=941819\&pmsEncounterId=13780398**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

\<?xml version="1.0" encoding="utf-16"?\>

\<Problems xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" name="Patient\_Problem" conceptType="List"\>

\<Patient\_Problem order="dateDescend" conceptName="Problems" conceptID="554:1000111/1000461" referenceID="**B4BBE498-4CF7-4758-9824-620C31495BE7**"\>

\<Patient\_Problem\_Comments conceptID="554:1000111/1000461/5" /\>

\<Patient\_Problem\_Description conceptID="554:1000111/1000461/2"\>**Acute myocardial infarction**\</Patient\_Problem\_Description\>

\<Patient\_Problem\_DateOfOnset conceptID="554:1000111/1000461/4" /\>

\<Patient\_Problem\_Code conceptID="554:1000111/1000461/3"\>**57054005**\</Patient\_Problem\_Code\>

\<Patient\_Problem\_CodingSystem conceptID="554:1000111/1000461/8"\>**SNOMED**\</Patient\_Problem\_CodingSystem\>

\<Patient\_Problem\_DateRecorded conceptID="554:1000111/1000461/1"\>**2019-01-31T00:00:00**\</Patient\_Problem\_DateRecorded\>

\<Patient\_Problem\_IsLongTerm conceptID="554:1000111/1000461"\>**true**\</Patient\_Problem\_IsLongTerm\>

\</Patient\_Problem\>

\<Patient\_Problem order="dateDescend" conceptName="Problems" conceptID="554:1000111/1000461" referenceID="**4FAFB9F8-D8F5-4ADB-879A-A004044E655B**"\>

\<Patient\_Problem\_Comments conceptID="554:1000111/1000461/5"\>**Testing only**\</Patient\_Problem\_Comments\>

\<Patient\_Problem\_Description conceptID="554:1000111/1000461/2"\>**Lung mass**\</Patient\_Problem\_Description\>

\<Patient\_Problem\_DateOfOnset conceptID="554:1000111/1000461/4" /\>

\<Patient\_Problem\_Code conceptID="554:1000111/1000461/3"\>**309529002**\</Patient\_Problem\_Code\>

\<Patient\_Problem\_CodingSystem conceptID="554:1000111/1000461/8"\>**SNOMED**\</Patient\_Problem\_CodingSystem\>

\<Patient\_Problem\_DateRecorded conceptID="554:1000111/1000461/1"\>**2019-01-31T00:00:00**\</Patient\_Problem\_DateRecorded\>

\<Patient\_Problem\_IsLongTerm conceptID="554:1000111/1000461"\>**true**\</Patient\_Problem\_IsLongTerm\>

\</Patient\_Problem\>

**\[...SNIP...\]**

\<Patient\_Problem order="dateDescend" conceptName="Problems" conceptID="554:1000111/1000461" referenceID="**B88A3757-C61B-43F3-B839-25B79028D2EA**"\>

\<Patient\_Problem\_Comments conceptID="554:1000111/1000461/5" /\>

\<Patient\_Problem\_Description conceptID="554:1000111/1000461/2"\>**Diabetic renal disease**\</Patient\_Problem\_Description\>

\<Patient\_Problem\_DateOfOnset conceptID="554:1000111/1000461/4"\>**2018-07-01T00:00:00**\</Patient\_Problem\_DateOfOnset\>

\<Patient\_Problem\_Code conceptID="554:1000111/1000461/3"\>**127013003**\</Patient\_Problem\_Code\>

\<Patient\_Problem\_CodingSystem conceptID="554:1000111/1000461/8"\>**SNOMED**\</Patient\_Problem\_CodingSystem\>

\<Patient\_Problem\_DateRecorded conceptID="554:1000111/1000461/1"\>**2018-11-07T00:00:00**\</Patient\_Problem\_DateRecorded\>

\<Patient\_Problem\_IsLongTerm conceptID="554:1000111/1000461"\>**false**\</Patient\_Problem\_IsLongTerm\>

\</Patient\_Problem\>

\</Problems\>

<span class="underline">With Minimum DateTime:</span>

**/getClassifications?pmsPatientId=941819\&pmsEncounterId=13780398\&pmsMinDateTime=2018-12-10**

\<?xml version="1.0" encoding="utf-16"?\>

\<Patient\_Problem order="dateDescend" minDateTime="**2018-12-10**" conceptName="Problems" conceptID="554:1000111/1000461" referenceID="**BE20E8C0-06FD-4293-BFDF-9A9BFF9D3D93**"\>

\<Patient\_Problem\_Comments conceptID="554:1000111/1000461/5"\>**G30..**\</Patient\_Problem\_Comments\>

\<Patient\_Problem\_Description conceptID="554:1000111/1000461/2"\>**Acute myocardial infarction**\</Patient\_Problem\_Description\>

\<Patient\_Problem\_DateOfOnset conceptID="554:1000111/1000461/4"\>**2018-12-11T00:00:00**\</Patient\_Problem\_DateOfOnset\>

\<Patient\_Problem\_Code conceptID="554:1000111/1000461/3"\>**57054005**\</Patient\_Problem\_Code\>

\<Patient\_Problem\_CodingSystem conceptID="554:1000111/1000461/8"\>**SNOMED**\</Patient\_Problem\_CodingSystem\>

\<Patient\_Problem\_DateRecorded conceptID="554:1000111/1000461/1"\>**2018-12-11T00:00:00**\</Patient\_Problem\_DateRecorded\>

\<Patient\_Problem\_IsLongTerm conceptID="554:1000111/1000461"\>**true**\</Patient\_Problem\_IsLongTerm\>

\</Patient\_Problem\>

\</Problems\>

## Regular Medications

API requirements: GET (read only)

Additional Parameters:

1.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

2.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

3.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns regular medication details (Long term) for the patient.

[**/getRegularMedications?pmsPatientId=941819\&pmsEncounterId=13780398**](https://vtlapi.spectrumpms.nz/api/getPatient?NHI=ABC1234)

\<?xml version="1.0" encoding="utf-16"?\>

\<RegularMedications xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" conceptType="List"\>

\<Patient\_RegularMedication order="dateDescend" referenceID="**301CC474-DBA8-40B3-914E-388F3C25CFC5**"\>

\<Patient\_RegularMedication\_StartedDate conceptID="554:1000111/1000501/7"\>**2019-02-07T00:00:00**\</Patient\_RegularMedication\_StartedDate\>

\<Patient\_RegularMedication\_Name conceptID="554:1000111/1000501/9"\>**ibrutinib 140 mg capsule**\</Patient\_RegularMedication\_Name\>

\<Patient\_RegularMedication\_Code conceptID="554:1000111/1000501/3"\>**45368761000116101**\</Patient\_RegularMedication\_Code\>

\<Patient\_RegularMedication\_CodingSystem conceptID="554:1000111/1000501/11"\>**SNOMED**\</Patient\_RegularMedication\_CodingSystem\>

\<Patient\_RegularMedication\_DispenseQuantity conceptID="554:1000111/1000501/4" /\>

\<Patient\_RegularMedication\_DispenseUnit conceptID="554:1000111/1000501/10" /\>

\<Patient\_RegularMedication\_DosageQuantity conceptID="554:1000111/1000501/14"\>**1**\</Patient\_RegularMedication\_DosageQuantity\>

\<Patient\_RegularMedication\_DosageUnit conceptID="554:1000111/1000501/16"\>**capsule**\</Patient\_RegularMedication\_DosageUnit\>

\<Patient\_RegularMedication\_Administrationinstructions conceptID="554:1000111/1000501/6"\>**Take 1 cap(s) Thrice Weekly**\</Patient\_RegularMedication\_Administrationinstructions\>

\<Patient\_RegularMedication\_LastPrescribedDate /\>

\</Patient\_RegularMedication\>

\<Patient\_RegularMedication order="dateDescend" referenceID="**04AC774C-3D85-445A-A2C9-AD72AD41E6CF**"\>

\<Patient\_RegularMedication\_StartedDate conceptID="554:1000111/1000501/7"\>**2019-01-30T00:00:00**\</Patient\_RegularMedication\_StartedDate\>

\<Patient\_RegularMedication\_Name conceptID="554:1000111/1000501/9"\>**Amzoate (sodium benzoate 100 mg/mL) oral liquid: solution**\</Patient\_RegularMedication\_Name\>

\<Patient\_RegularMedication\_Code conceptID="554:1000111/1000501/3"\>**10044301000116109**\</Patient\_RegularMedication\_Code\>

\<Patient\_RegularMedication\_CodingSystem conceptID="554:1000111/1000501/11"\>**SNOMED**\</Patient\_RegularMedication\_CodingSystem\>

\<Patient\_RegularMedication\_DispenseQuantity conceptID="554:1000111/1000501/4" /\>

\<Patient\_RegularMedication\_DispenseUnit conceptID="554:1000111/1000501/10" /\>

\<Patient\_RegularMedication\_DosageQuantity conceptID="554:1000111/1000501/14"\>**3**\</Patient\_RegularMedication\_DosageQuantity\>

\<Patient\_RegularMedication\_DosageUnit conceptID="554:1000111/1000501/16"\>**mL**\</Patient\_RegularMedication\_DosageUnit\>

\<Patient\_RegularMedication\_Administrationinstructions conceptID="554:1000111/1000501/6"\>**Take 3 ml(s) 5 Times Day**\</Patient\_RegularMedication\_Administrationinstructions\>

\<Patient\_RegularMedication\_LastPrescribedDate /\>

\</Patient\_RegularMedication\>

**\[…SNIP…\]**

\</RegularMedications\>

## Prescribed Medications

API requirements: GET (read only)

Additional Parameters:

1.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

2.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

3.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns prescribed medication details (Short term) for the patient.

**/getPrescribedMedications?pmsPatientId=941819\&pmsEncounterId=13780398**

\<?xml version="1.0" encoding="utf-16"?\>

\<PrescribedMedications xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" conceptType="List"\>

\<Patient\_PrescribedMedication order="dateDescend" referenceID="**0B6797BB-E20C-495F-9871-75C68004710A**"\>

\<Patient\_PrescribedMedication\_StartedDate conceptID="554:1000111/1000501/7"\>**2018-11-16T00:00:00**\</Patient\_PrescribedMedication\_StartedDate\>

\<Patient\_PrescribedMedication\_Name conceptID="554:1000111/1000501/9"\>**paracetamol 500 mg + codeine phosphate 8 mg tablet**\</Patient\_PrescribedMedication\_Name\>

\<Patient\_PrescribedMedication\_Code conceptID="554:1000111/1000501/3"\>**10059441000116102**\</Patient\_PrescribedMedication\_Code\>

\<Patient\_PrescribedMedication\_CodingSystem conceptID="554:1000111/1000501/11"\>**SNOMED**\</Patient\_PrescribedMedication\_CodingSystem\>

\<Patient\_PrescribedMedication\_DispenseQuantity conceptID="554:1000111/1000501/4" /\>

\<Patient\_PrescribedMedication\_DispenseUnit conceptID="554:1000111/1000501/10" /\>

\<Patient\_PrescribedMedication\_DosageQuantity conceptID="554:1000111/1000501/14"\>**1**\</Patient\_PrescribedMedication\_DosageQuantity\>

\<Patient\_PrescribedMedication\_DosageUnit conceptID="554:1000111/1000501/16"\>**tablet**\</Patient\_PrescribedMedication\_DosageUnit\>

\<Patient\_PrescribedMedication\_Administrationinstructions conceptID="554:1000111/1000501/6"\>**Take 1 tab(s) Once Daily**\</Patient\_PrescribedMedication\_Administrationinstructions\>

\<Patient\_PrescribedMedication\_LastPrescribedDate conceptID="554:1000111/1000501/12"\>**2018-11-16T00:00:00**\</Patient\_PrescribedMedication\_LastPrescribedDate\>

\</Patient\_PrescribedMedication\>

\<Patient\_PrescribedMedication order="dateDescend" referenceID="**0BDA3B3F-D86F-401F-83CD-930B2F904C3B**"\>

\<Patient\_PrescribedMedication\_StartedDate conceptID="554:1000111/1000501/7"\>**2018-11-16T00:00:00**\</Patient\_PrescribedMedication\_StartedDate\>

\<Patient\_PrescribedMedication\_Name conceptID="554:1000111/1000501/9"\>**ibrutinib 140 mg capsule**\</Patient\_PrescribedMedication\_Name\>

\<Patient\_PrescribedMedication\_Code conceptID="554:1000111/1000501/3"\>**45368761000116101**\</Patient\_PrescribedMedication\_Code\>

\<Patient\_PrescribedMedication\_CodingSystem conceptID="554:1000111/1000501/11"\>**SNOMED**\</Patient\_PrescribedMedication\_CodingSystem\>

\<Patient\_PrescribedMedication\_DispenseQuantity conceptID="554:1000111/1000501/4" /\>

\<Patient\_PrescribedMedication\_DispenseUnit conceptID="554:1000111/1000501/10" /\>

\<Patient\_PrescribedMedication\_DosageQuantity conceptID="554:1000111/1000501/14"\>**1**\</Patient\_PrescribedMedication\_DosageQuantity\>

\<Patient\_PrescribedMedication\_DosageUnit conceptID="554:1000111/1000501/16"\>**capsule**\</Patient\_PrescribedMedication\_DosageUnit\>

\<Patient\_PrescribedMedication\_Administrationinstructions conceptID="554:1000111/1000501/6"\>**Take 1 cap(s) Once Daily**\</Patient\_PrescribedMedication\_Administrationinstructions\>

\<Patient\_PrescribedMedication\_LastPrescribedDate conceptID="554:1000111/1000501/12"\>**2018-11-16T00:00:00**\</Patient\_PrescribedMedication\_LastPrescribedDate\>

\</Patient\_PrescribedMedication\>

\</PrescribedMedications\>

## Consult Notes

API requirements: GET (read only)

Additional Parameters:

1.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

2.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

3.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns consult notes for patient that includes subjective and objective comments.

**/getConsultNotes?pmsPatientId=941819\&pmsEncounterId=13780398**

\<?xml version="1.0" encoding="utf-16"?\>

\<ConsultNotes xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" conceptType="List"\>

\<Patient\_Consult order="dateDescend" referenceID="**825F7494-A201-473C-AFBC-6909F160FE1B**"\>

\<Patient\_Consult\_Date conceptID="554:1000111/1000156/6"\>**2019-01-28T08:48:03**\</Patient\_Consult\_Date\>

\<Patient\_Consult\_Exam conceptID="554:1000111/1000156/3"\>**Test Obj 2**\</Patient\_Consult\_Exam\>

\<Patient\_Consult\_History conceptID="554:1000111/1000156/1"\>**Test Sub 2**\</Patient\_Consult\_History\>

\<Patient\_Consult\_Assessment conceptID="554:1000111/1000156/99"\>**Test Assess 2**\</Patient\_Consult\_Assessment\>

\<Patient\_Consult\_Plan conceptID="554:1000111/1000156/98"\>**Test Plan 2**\</Patient\_Consult\_Plan\>

\</Patient\_Consult\>

**\[…SNIP…\]**

\<Patient\_Consult order="dateDescend" referenceID="**2E9466DA-E109-417F-A1BA-8203AD7534A7**"\>

\<Patient\_Consult\_Date conceptID="554:1000111/1000156/6"\>**2018-11-30T19:50:16**\</Patient\_Consult\_Date\>

\<Patient\_Consult\_Exam conceptID="554:1000111/1000156/3"\>**This is objective test notes**\</Patient\_Consult\_Exam\>

\<Patient\_Consult\_History conceptID="554:1000111/1000156/1"\>**This is subjective test notes**\</Patient\_Consult\_History\>

\<Patient\_Consult\_Assessment conceptID="554:1000111/1000156/99" /\>

\<Patient\_Consult\_Plan conceptID="554:1000111/1000156/98" /\>

\</Patient\_Consult\>

\</ConsultNotes\>

## Next of Kin

API requirements: GET (read only)

### Get

Returns contact details of Next of kin against the patient.

**/getNextOfKin?pmsPatientId=941819\&pmsEncounterId=13780398**

\<?xml version="1.0" encoding="utf-16"?\>

\<Next\_Of\_Kin xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" conceptType="List"\>

\<PatientNOK conceptID="554:1000111/1000760" referenceID="**297226**"\>

\<PatientNOK\_Address\_AdditionalLine conceptID="554:1000111/1000760/4" /\>

\<PatientNOK\_Address\_City conceptID="554:1000111/1000760/7"\>**Auckland**\</PatientNOK\_Address\_City\>

\<PatientNOK\_Address\_Postcode conceptID="554:1000111/1000760/6"\>**4395**\</PatientNOK\_Address\_Postcode\>

\<PatientNOK\_Address\_StreetName conceptID="554:1000111/1000760/3"\>**Great South Road** \</PatientNOK\_Address\_StreetName\>

\<PatientNOK\_Address\_StreetNumber conceptID="554:1000111/1000760/2"\>**585**\</PatientNOK\_Address\_StreetNumber\>

\<PatientNOK\_Address\_Suburb conceptID="554:1000111/1000760/5"\>**Penrose**\</PatientNOK\_Address\_Suburb\>

\<PatientNOK\_Firstname conceptID="554:1000111/1000710/1"\>**Patient F2**\</PatientNOK\_Firstname\>

\<PatientNOK\_Middlename conceptID="554:1000111/1000710/3" /\>

\<PatientNOK\_Surname conceptID="554:1000111/1000710/2"\>**NOK**\</PatientNOK\_Surname\>

\<PatientNOK\_Mobile conceptID="554:1000111/1000731"\>**+64211234567**\</PatientNOK\_Mobile\>

\<PatientNOK\_PreferredNumber /\>

\<PatientNOK\_Relationship conceptID="554:1000111/1000710/5"\>**Wife**\</PatientNOK\_Relationship\>

\<PatientNOK\_ResidentialPhone conceptID="554:1000111/1000730"\>**+64217654321**\</PatientNOK\_ResidentialPhone\>

\<PatientNOK\_WorkPhone conceptID="554:1000111/1000732" /\>

\<PatientNOK\_IsDefault conceptID="554:1000111/1000710/6"\>**true**\</PatientNOK\_IsDefault\>

\</PatientNOK\>

\<PatientNOK conceptID="554:1000111/1000760" referenceID="**297227**"\>

\<PatientNOK\_Address\_AdditionalLine conceptID="554:1000111/1000760/4" /\>

\<PatientNOK\_Address\_City conceptID="554:1000111/1000760/7"\>**Hamilton**\</PatientNOK\_Address\_City\>

\<PatientNOK\_Address\_Postcode conceptID="554:1000111/1000760/6" /\>

\<PatientNOK\_Address\_StreetName conceptID="554:1000111/1000760/3"\>**Little London Lane**\</PatientNOK\_Address\_StreetName\>

\<PatientNOK\_Address\_StreetNumber conceptID="554:1000111/1000760/2"\>**10A**\</PatientNOK\_Address\_StreetNumber\>

\<PatientNOK\_Address\_Suburb conceptID="554:1000111/1000760/5"\>**Hamilton Central**\</PatientNOK\_Address\_Suburb\>

\<PatientNOK\_Firstname conceptID="554:1000111/1000710/1"\>**Patient F**\</PatientNOK\_Firstname\>

\<PatientNOK\_Middlename conceptID="554:1000111/1000710/3" /\>

\<PatientNOK\_Surname conceptID="554:1000111/1000710/2"\>**NOK**\</PatientNOK\_Surname\>

\<PatientNOK\_Mobile conceptID="554:1000111/1000731"\>**+64211234567**\</PatientNOK\_Mobile\>

\<PatientNOK\_PreferredNumber /\>

\<PatientNOK\_Relationship conceptID="554:1000111/1000710/5"\>**Mother**\</PatientNOK\_Relationship\>

\<PatientNOK\_ResidentialPhone conceptID="554:1000111/1000730" /\>

\<PatientNOK\_WorkPhone conceptID="554:1000111/1000732" /\>

\<PatientNOK\_IsDefault conceptID="554:1000111/1000710/6"\>**false**\</PatientNOK\_IsDefault\>

\</PatientNOK\>

\</Next\_Of\_Kin\>

## Allergies/Warnings

API requirements: GET (read only)

Additional Parameters:

1.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

2.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

3.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns details on allergies for the patient.

**/getMedicalAllergies?pmsPatientId=941819\&pmsEncounterId=13780398**

\<?xml version="1.0" encoding="utf-16"?\>

\<MedicalWarnings xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" conceptType="List"\>

\<Patient\_MedicalWarning order="dateDescend" referenceID="**2397C0EA-803D-4694-85B7-EE3014374F54**"\>

\<Patient\_MedicalWarning\_Comments conceptID="554:1000111/1000490/6"\>**Rash on upper body-Test medical warning**\</Patient\_MedicalWarning\_Comments\>

\<Patient\_MedicalWarning\_Date conceptID="554:1000111/1000490/1"\>**2019-01-20T00:00:00**\</Patient\_MedicalWarning\_Date\>

\<Patient\_MedicalWarning\_Description conceptID="554:1000111/1000490/3"\>**Caffeine + paracetamol (medicinal product)-Allergy**\</Patient\_MedicalWarning\_Description\>

\<Patient\_MedicalWarning\_RecordedByID conceptID="554:1000111/1000490/2"\>**941823**\</Patient\_MedicalWarning\_RecordedByID\>

\<Patient\_MedicalWarning\_Category /\>

\<Patient\_MedicalWarning\_Reaction /\>

\<Patient\_MedicalWarning\_Severity /\>

\</Patient\_MedicalWarning\>

**\[...SNIP...\]**

\<Patient\_MedicalWarning order="dateDescend" referenceID="**3B4C853F-76AA-446E-B88F-BCBA90E43D1B**"\>

\<Patient\_MedicalWarning\_Comments conceptID="554:1000111/1000490/6" /\>

\<Patient\_MedicalWarning\_Date conceptID="554:1000111/1000490/1"\>**2019-02-01T13:09:34**\</Patient\_MedicalWarning\_Date\>

\<Patient\_MedicalWarning\_Description conceptID="554:1000111/1000490/3"\>**Chlorphenamine + codeine + paracetamol + phenylephrine (medicinal product)-Allergy**\</Patient\_MedicalWarning\_Description\>

\<Patient\_MedicalWarning\_RecordedByID conceptID="554:1000111/1000490/2"\>**941823**\</Patient\_MedicalWarning\_RecordedByID\>

\<Patient\_MedicalWarning\_Category /\>

\<Patient\_MedicalWarning\_Reaction /\>

\<Patient\_MedicalWarning\_Severity /\>

\</Patient\_MedicalWarning\>

\</MedicalWarnings\>

## Registered Practitioners

API requirements: GET (read only)

### Get

Returns list of registered practitioners at the practice.

**/getRegisteredPractitioners?pmsPatientId=941819\&pmsEncounterId=13780398\&pmsLocationId=11**

\<?xml version="1.0" encoding="utf-16"?\>

\<RegisteredPractitioners xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" conceptType="List"\>

\<RegisteredPractitioner referenceID="**940858**"\>

\<RegisteredPractitioner\_Title conceptID="554:1000123/1000610/4" /\>

\<RegisteredPractitioner\_FirstName conceptID="554:1000123/1000610/1"\>**Aarvid**\</RegisteredPractitioner\_FirstName\>

\<RegisteredPractitioner\_Surname conceptID="554:1000123/1000610/2"\>**Gatz**\</RegisteredPractitioner\_Surname\>

\<RegisteredPractitioner\_FullName conceptID="554:1000123/1000610/7"\>**Aarvid GATZ**\</RegisteredPractitioner\_FullName\>

\<RegisteredPractitioner\_RegistrationNumber conceptID="554:1000123/1000613"\>**77688099-0**\</RegisteredPractitioner\_RegistrationNumber\>

\<RegisteredPractitioner\_RegisteringBody conceptID="554:1000123/1000619"\>**NZNC**\</RegisteredPractitioner\_RegisteringBody\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetNumber conceptID="554:1000111/1000740/2"\>**401 Pegasus House Madrass Street**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetNumber\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetName conceptID="554:1000111/1000740/3"\>**Madrass Street**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetName\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_Suburb conceptID="554:1000111/1000740/5"\>**Addington**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_Suburb\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_City conceptID="554:1000111/1000740/7"\>**Christchurch**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_City\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_Postcode conceptID="554:1000111/1000740/6"\>**8011**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_Postcode\>

\<RegisteredPractitionerOrganisation\_Phone conceptID="554:1000111/1000741" /\>

\<RegisteredPractitionerOrganisation\_Fax conceptID="554:1000111/1000742" /\>

\<RegisteredPractitionerOrganisation\_FacilityHPI conceptID="554:1000111/1000745"\>**F2M067**\</RegisteredPractitionerOrganisation\_FacilityHPI\>

\<RegisteredPractitionerOrganisation\_HealthLinkEDI conceptID="554:1000123/1000612"\>**n28n6ujh**\</RegisteredPractitionerOrganisation\_HealthLinkEDI\>

\<RegisteredPractitioner\_PMSID conceptID="554:1000123/1000620"\>**940858**\</RegisteredPractitioner\_PMSID\>

\<RegisteredPractitioner\_Email conceptID="554:1000123/1000623" /\>

\<RegisteredPractitioner\_PersonalHPI conceptID="554:1000123/1000614" /\>

\</RegisteredPractitioner\>

**...\[SNIP\]...**

\<RegisteredPractitioner referenceID="941644"\>

\<RegisteredPractitioner\_Title conceptID="554:1000123/1000610/4"\>**Dr**\</RegisteredPractitioner\_Title\>

\<RegisteredPractitioner\_FirstName conceptID="554:1000123/1000610/1"\>**William**\</RegisteredPractitioner\_FirstName\>

\<RegisteredPractitioner\_Surname conceptID="554:1000123/1000610/2"\>**Hughes**\</RegisteredPractitioner\_Surname\>

\<RegisteredPractitioner\_FullName conceptID="554:1000123/1000610/7"\>**William** **HUGHES**\</RegisteredPractitioner\_FullName\>

\<RegisteredPractitioner\_RegistrationNumber conceptID="554:1000123/1000613"\>**WIL12345**\</RegisteredPractitioner\_RegistrationNumber\>

\<RegisteredPractitioner\_RegisteringBody conceptID="554:1000123/1000619"\>**NZMC**\</RegisteredPractitioner\_RegisteringBody\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetNumber conceptID="554:1000111/1000740/2"\>**401 Pegasus House Madrass Street**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetNumber\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetName conceptID="554:1000111/1000740/3"\>**Madrass Street**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetName\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_Suburb conceptID="554:1000111/1000740/5"\>**Addington**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_Suburb\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_City conceptID="554:1000111/1000740/7"\>**Christchurch**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_City\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_Postcode conceptID="554:1000111/1000740/6"\>**8011**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_Postcode\>

\<RegisteredPractitionerOrganisation\_Phone conceptID="554:1000111/1000741" /\>

\<RegisteredPractitionerOrganisation\_Fax conceptID="554:1000111/1000742" /\>

\<RegisteredPractitionerOrganisation\_FacilityHPI conceptID="554:1000111/1000745"\>**F2M067**\</RegisteredPractitionerOrganisation\_FacilityHPI\>

\<RegisteredPractitionerOrganisation\_HealthLinkEDI conceptID="554:1000123/1000612"\>**n28n6ujh**\</RegisteredPractitionerOrganisation\_HealthLinkEDI\>

\<RegisteredPractitioner\_PMSID conceptID="554:1000123/1000620"\>**941644**\</RegisteredPractitioner\_PMSID\>

\<RegisteredPractitioner\_Email conceptID="554:1000123/1000623"\>**william.hughes@ventures.health.nz**\</RegisteredPractitioner\_Email\>

\<RegisteredPractitioner\_PersonalHPI conceptID="554:1000123/1000614" /\>

\</RegisteredPractitioner\>

\<RegisteredPractitioner referenceID="446869"\>

\<RegisteredPractitioner\_Title conceptID="554:1000123/1000610/4"\>**LADY**\</RegisteredPractitioner\_Title\>

\<RegisteredPractitioner\_FirstName conceptID="554:1000123/1000610/1"\>**Zoe**\</RegisteredPractitioner\_FirstName\>

\<RegisteredPractitioner\_Surname conceptID="554:1000123/1000610/2"\>**Pickering**\</RegisteredPractitioner\_Surname\>

\<RegisteredPractitioner\_FullName conceptID="554:1000123/1000610/7"\>**Zoe** **PICKERING**\</RegisteredPractitioner\_FullName\>

\<RegisteredPractitioner\_RegistrationNumber conceptID="554:1000123/1000613"\>**123456**\</RegisteredPractitioner\_RegistrationNumber\>

\<RegisteredPractitioner\_RegisteringBody conceptID="554:1000123/1000619"\>**NZMC**\</RegisteredPractitioner\_RegisteringBody\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetNumber conceptID="554:1000111/1000740/2"\>**401 Pegasus House Madrass Street**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetNumber\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetName conceptID="554:1000111/1000740/3"\>**Madrass** **Street**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_StreetName\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_Suburb conceptID="554:1000111/1000740/5"\>**Addington**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_Suburb\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_City conceptID="554:1000111/1000740/7"\>**Christchurch**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_City\>

\<RegisteredPractitionerOrganisation\_PhysicalAddress\_Postcode conceptID="554:1000111/1000740/6"\>**8011**\</RegisteredPractitionerOrganisation\_PhysicalAddress\_Postcode\>

\<RegisteredPractitionerOrganisation\_Phone conceptID="554:1000111/1000741"\>**0225190973**\</RegisteredPractitionerOrganisation\_Phone\>

\<RegisteredPractitionerOrganisation\_Fax conceptID="554:1000111/1000742" /\>

\<RegisteredPractitionerOrganisation\_FacilityHPI conceptID="554:1000111/1000745"\>**F2M067**\</RegisteredPractitionerOrganisation\_FacilityHPI\>

\<RegisteredPractitionerOrganisation\_HealthLinkEDI conceptID="554:1000123/1000612"\>**n28n6ujh**\</RegisteredPractitionerOrganisation\_HealthLinkEDI\>

\<RegisteredPractitioner\_PMSID conceptID="554:1000123/1000620"\>**446869**\</RegisteredPractitioner\_PMSID\>

\<RegisteredPractitioner\_Email conceptID="554:1000123/1000623" /\>

\<RegisteredPractitioner\_PersonalHPI conceptID="554:1000123/1000614" /\>

\</RegisteredPractitioner\>

\</RegisteredPractitioners\>

## Smoking Status

API requirements: GET (read only)

### Get

Returns data for Smoking status for the patient.

**/getSmokingStatus?pmsPatientId=941819\&pmsEncounterId=13780398**

\<?xml version="1.0" encoding="utf-16"?\>

\<SmokingStatus xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" conceptType="List"\>

\<Patient\_Smoking referenceID="**941819**"\>

\<Patient\_Smoking\_ConsumptionDescription conceptID="554:1000111/1000471/4"\>**Quit within last year**\</Patient\_Smoking\_ConsumptionDescription\>

\<Patient\_Smoking\_Code conceptID="554:1000111/1000471/7"\>**137G**\</Patient\_Smoking\_Code\>

\<Patient\_Smoking\_CodingSystem /\>

\<Patient\_Smoking\_Date conceptID="554:1000111/1000471/1"\>**2018-12-14T02:03:40.977**\</Patient\_Smoking\_Date\>

\</Patient\_Smoking\>

\</SmokingStatus\>

## Accidents

API requirements: GET (read only)

Additional Parameters:

1.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

2.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

3.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns patient accident (ACC45) data.

**/getAccidents?pmsPatientId=941819\&pmsEncounterId=13780398**

\<?xml version="1.0" encoding="utf-16"?\>

\<Accidents xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" conceptType="List"\>

\<Patient\_Accident referenceID="**624500ED-D30E-451A-9EA3-1D1215A9FB72**"\>

\<Patient\_Accident\_RegistrationNumber conceptID="554:1000111/1000300/2"\>**AY12345**\</Patient\_Accident\_RegistrationNumber\>

\<Patient\_Accident\_Date conceptID="554:1000111/1000300/1"\>**2018-12-14T01:37:39.217**\</Patient\_Accident\_Date\>

\<Patient\_Accident\_DiagnosisDescription conceptID="554:1000111/1000300/4"\>**Sprain of knee - Injury when trying to dance on stairs**\</Patient\_Accident\_DiagnosisDescription\>

\<Patient\_Accident\_IsWorkRelated conceptID="554:1000111/1000300/5"\>**true**\</Patient\_Accident\_IsWorkRelated\>

\<Patient\_Accident\_Location\_Description conceptID="554:1000111/1000300/6" /\>

\</Patient\_Accident\>

\</Accidents\>

## Measurements

API requirements: GET (read only)

### Get

Returns patient measurement data.

**/getPatientMeasurement?pmsPatientId=941819\&pmsEncounterId=13780398**

\<?xml version="1.0" encoding="utf-16"?\>

\<Patient\_Measurement xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" name="measurements"\>

\<Measurement\_BP\_SYS qualifierName="systolic" qualifierID="**271649006**" conceptID="554:1000111/1002031" name="measurement.bloodPressure.systolic" dateTaken="**2018-11-16T22:41:02**"\>**120**\</Measurement\_BP\_SYS\>

\<Measurement\_BP\_DIA qualifierName="diastolic" qualifierID="**271650006**" conceptID="554:1000111/1002031" name="measurement.bloodPressure.diastolic" dateTaken="**2018-11-16T22:41:02**"\>**70**\</Measurement\_BP\_DIA\>

\<Measurement\_Weight qualifierName="body weight" qualifierID="**27113001**" conceptID="554:1000111/1002031" name="measurement.weight" dateTaken="**2018-11-16T22:42:20**"\>**70**\</Measurement\_Weight\>

\<Measurement\_Height qualifierName="body height" qualifierID="**50373000**" conceptID="554:1000111/1002031" name="measurement.height" dateTaken="**2018-11-16T22:41:54**"\>**180**\</Measurement\_Height\>

\<Measurement\_BMI qualifierName="body mass index" qualifierID="**60621009**" conceptID="554:1000111/1002031" name="measurement.bmi" dateTaken="**2018-11-16T22:41:28**"\>**21.6**\</Measurement\_BMI\>

\</Patient\_Measurement\>

## Lab Reports Listing

API requirements: GET (read only)

Additional Parameters:

1.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

2.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

3.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns patient lab reports list. Content call is separate.

**/getLaboratoryReportList?pmsPatientId=941819\&pmsEncounterId=13781624**

\<?xml version="1.0" encoding="utf-16"?\>

\<LaboratoryReports xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" conceptType="List"\>

\<Patient\_LaboratoryReport order="dateDescend" referenceID="**EA1C177A-D4C6-4ED7-AFE8-CD424F81836E**"\>

\<Patient\_LaboratoryReport\_SendingFacility conceptID="554:1000111/1000450/2"\>**pathlabs**\</Patient\_LaboratoryReport\_SendingFacility\>

\<Patient\_LaboratoryReport\_Subject conceptID="554:1000111/1000450/3"\>**HBA1c**\</Patient\_LaboratoryReport\_Subject\>

\<Patient\_LaboratoryReport\_Name conceptID="554:1000111/1000450/17"\>**HBA1c**\</Patient\_LaboratoryReport\_Name\>

\<Patient\_LaboratoryReport\_Date\_Received conceptID="554:1000111/1000450/1"\>**2017-07-29T01:54:42**\</Patient\_LaboratoryReport\_Date\_Received\>

\<Patient\_LaboratoryReport\_DataType conceptID="554:1000111/1000450/15"\>**application/rtf**\</Patient\_LaboratoryReport\_DataType\>

\<Patient\_LaboratoryReport\_Comments conceptID="554:1000111/1000450/27" /\>

\</Patient\_LaboratoryReport\>

\<Patient\_LaboratoryReport order="dateDescend" referenceID="**FBE36EC6-7DCA-4C8F-AA82-FA2066C8E56C**"\>

\<Patient\_LaboratoryReport\_SendingFacility conceptID="554:1000111/1000450/2"\>**pathlabs**\</Patient\_LaboratoryReport\_SendingFacility\>

\<Patient\_LaboratoryReport\_Subject conceptID="554:1000111/1000450/3"\>**TFTs............**\</Patient\_LaboratoryReport\_Subject\>

\<Patient\_LaboratoryReport\_Name conceptID="554:1000111/1000450/17"\>**TFTs............**\</Patient\_LaboratoryReport\_Name\>

\<Patient\_LaboratoryReport\_Date\_Received conceptID="554:1000111/1000450/1"\>**2017-07-29T01:54:22**\</Patient\_LaboratoryReport\_Date\_Received\>

\<Patient\_LaboratoryReport\_DataType conceptID="554:1000111/1000450/15"\>**application/rtf**\</Patient\_LaboratoryReport\_DataType\>

\<Patient\_LaboratoryReport\_Comments conceptID="554:1000111/1000450/27" /\>

\</Patient\_LaboratoryReport\>

**\[…SNIP…\]**

\</Patient\_LaboratoryReport\>

\</LaboratoryReports\>

## Lab Report Details

API requirements: GET (read only)

Additional Parameters:

1.  Reference Id – Identifier from Lab Report Listing concept to ID a lab entry

### Get

Returns patient lab report data for given reference.

**/getLaboratoryReportDetails?pmsPatientId=941819\&pmeEncounterId=13781624\&pmsReferenceId=B160D6C5-62EF-4AF8-B2D7-174F675AB6AC**

\<?xml version="1.0" encoding="utf-16"?\>

\<LaboratoryReportsContent xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" name="clinical.diagnosticReports"\>

\<Patient\_LaboratoryReport referenceID="**B160D6C5-62EF-4AF8-B2D7-174F675AB6AC**"\>

\<Patient\_LaboratoryReport\_Content conceptID="554:1000111/1000450/4"\>**DQoNCkNob2xlc3Rlc...\[SNIP\]...mlkZToJMS40IG1tb2wvTCA=**\</Patient\_LaboratoryReport\_Content\>

\</Patient\_LaboratoryReport\>

\</LaboratoryReportsContent\>

## Radiology Reports Listing

API requirements: GET (read only)

Additional Parameters:

1.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

2.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

3.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns patient radiology reports list. Content call is separate.

**/getRadiologyReportList?pmsPatientId=941819\&pmsEncounterId=13781624**

\<?xml version="1.0" encoding="utf-16"?\>

\<RadiologyReports xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" name="clinical.RadiologyReport" conceptType="List"\>

\<group order="dateDescend" referenceID="**F9765291-30A3-45DB-91A0-6AE6077415F3**"\>

\<RadiologyReport\_SendingFacility conceptID="554:1000111/1000454/2"\>**hamradio**\</RadiologyReport\_SendingFacility\>

\<RadiologyReport\_Subject conceptID="554:1000111/1000454/3"\>**Radiology X-RAY**\</RadiologyReport\_Subject\>

\<RadiologyReport\_Name conceptID="554:1000111/1000450/17"\>**Radiology X-RAY**\</RadiologyReport\_Name\>

\<RadiologyReport\_DateCreated conceptID="554:1000111/1000454/17"\>**2017-11-22T00:34:41**\</RadiologyReport\_DateCreated\>

\<RadiologyReport\_DataType conceptID="554:1000111/1000454/15"\>**application/rtf**\</RadiologyReport\_DataType\>

\<RadiologyReport\_Comments conceptID="554:1000111/1000454/18" /\>

\</group\>

\</RadiologyReports\>

## Radiology Reports Details

API requirements: GET (read only)

Additional Parameters:

1.  Reference Id – Identifier from Radiology Report Listing concept to ID a rad entry

### Get

Returns patient radiology reports data.

**/getRadiologyReportDetails?pmsPatientId=941819\&pmsEncounterId=13781624\&pmsReferenceId=F9765291-30A3-45DB-91A0-6AE6077415F3**

\<?xml version="1.0" encoding="utf-16"?\>

\<RadiologyReportsContent xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" name="clinical.RadiologyReport"\>

\<Patient\_RadiologyReport referenceID="**F9765291-30A3-45DB-91A0-6AE6077415F3**"\>

\<Patient\_RadiologyReport\_Content conceptID="554:1000111/1000454/4"\>**VGhpcyByZXBvcnQgaXMgZm…\[SNIP\]…2xvZ2lzdDogRHIgSi4gRHVtYmxlDQo=**\</Patient\_RadiologyReport\_Content\>

\</Patient\_RadiologyReport\>

\</RadiologyReportsContent\>

## Discharge Summary Listing

API requirements: GET (read only)

Additional Parameters:

1.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

2.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

3.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns patient discharge summary list. Content call is separate.

**/getDischargeSummaryReportList?pmsPatientId=941819\&pmsEncounterId=13781624**

\<?xml version="1.0" encoding="utf-16"?\>

\<DischargeReports xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" name="clinical.DischargeReports" conceptType="List"\>

\<group order="dateDescend" referenceID="**1119D357-0800-4740-BBC7-F21B03112233**"\>

\<DischargeReport\_SendingFacility conceptID="554:1000111/1000451/2"\>**lakesdhb**\</DischargeReport\_SendingFacility\>

\<DischargeReport\_Subject conceptID="554:1000111/1000451/3"\>**Discharge Summary**\</DischargeReport\_Subject\>

\<DischargeReport\_Name conceptID="554:1000111/1000451/17"\>**Discharge Summary**\</DischargeReport\_Name\>

\<DischargeReport\_DateReceived conceptID="554:1000111/1000451/1"\>**2017-02-08T06:01:13**\</DischargeReport\_DateReceived\>

\<DischargeReport\_DataType conceptID="554:1000111/1000451/15"\>**application/rtf**\</DischargeReport\_DataType\>

\<DischargeReport\_Comments conceptID="554:1000111/1000451/27" /\>

\</group\>

\</DischargeReports\>

## Discharge Summary Details

API requirements: GET (read only)

Additional Parameters:

1.  Reference Id – Identifier from Discharge Summary Listing concept to ID a single entry

### Get

Returns patient discharge summary data.

**/getDischargeSummaryDetails?pmsPatientId=941819\&pmsEncounterId=13781624\&pmsReferenceId=1119D357-0800-4740-BBC7-F21B03112233**

\<?xml version="1.0" encoding="utf-16"?\>

\<DischargeSummaryContents xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" name="clinical.DischargeReport"\>

\<Patient\_DischargeSummary referenceID="**0A1C7D8E-9D24-4933-B779-71D403485922**"\>

\<Patient\_DischargeSummary\_Content conceptID="554:1000111/1000451/4"\>**e1xydGYxXGFuc2lcYW5za...\[SNIP\]...XGZzMjJccGFyDQp9DQoA**\</Patient\_DischargeSummary\_Content\>

\</Patient\_DischargeSummary\>

\</DischargeSummaryContents\>

## Save/Upload Document

API requirements: POST

Additional Node:

1.  *ReferralDocument\_Encounter\_ID* – Is the relevant Encouter Identity supplied in the Invocation URI

2.  *ReferralDocument\_Item\_Type* – Should either be “out” for Outbox or “in” for Inbox without the quotes

### POST

Saves a document using its base64 value passed with details of its type, targeted destination (inbox/outbox) and other details like Referral information.

Note: The POST method is not restricted to just Referrals, the support is for any document (base64) with the required parameters filled in.

It follows the write back rules defined in “*<span class="underline">ERMS Integration with PMS - Generic Tech Spec.pdf</span>*” section **3.15**.

**/saveDocument**

\<ReferralDocument\>

\<ReferralDocument\_Referral\_ID\>**ERMS-14541**\</ReferralDocument\_Referral\_ID\>

\<ReferralDocument\_Document\_ID\>**10001**\</ReferralDocument\_Document\_ID\>

\<ReferralDocument\_Patient\_PMS\_ID\>**941819**\</ReferralDocument\_Patient\_PMS\_ID\>

\<ReferralDocument\_Encounter\_ID\>**13781624**\</ReferralDocument\_Encounter\_ID\>

\<ReferralDocument\_Item\_Type\>**out**\</ReferralDocument\_Item\_Type\>

\<ReferralDocument\_Referral\_Type\>**Community Nursing Referral**\</ReferralDocument\_Referral\_Type\>

\<ReferralDocument\_Referral\_Status\>**Parked**\</ReferralDocument\_Referral\_Status\>

\<ReferralDocument\_Created\_Date\>**13/05/2015**\</ReferralDocument\_Created\_Date\>

\<ReferralDocument\_Referrer\_Fullname\>**Dr Stephen Lewis**\</ReferralDocument\_Referrer\_Fullname\>

\<ReferralDocument\_Referrer\_PMS\_ID\>**941823**\</ReferralDocument\_Referrer\_PMS\_ID\>

\<ReferralDocument\_Document\_Source\>**ERMS**\</ReferralDocument\_Document\_Source\>

\<ReferralDocument\_Content\_Type\>**PDF**\</ReferralDocument\_Content\_Type\>

\<ReferralDocument\_Description\_Type\>**RTF**\</ReferralDocument\_Description\_Type\>

\<ReferralDocument\_Description\>**This data tells something about the attachment, plain text only**\</ReferralDocument\_Description\>

\<ReferralDocument\_Encoding\>**BASE64**\</ReferralDocument\_Encoding\>

\<ReferralDocument\_Content\>**JVBERi0xLjUNCjIgMCBvYmoNCjw8IC9GaWx0ZXIgL0ZsYXR...\[SNIP\]...JSVFT0YNCg==**\</ReferralDocument\_Content\>

\<ReferralDocument\_Error\_Text/\>

\</ReferralDocument\>

## Scanned Document Listing

API requirements: GET (read only)

Additional Parameters:

4.  Minimum DateTime – Optional (e.g. \&minDateTime=**2018-11-16**)

5.  Maximum DateTime – Optional (e.g. \&maxDateTime=**2018-11-20**)

6.  Sort Order – Optional (e.g. \&order=**desc**)

### Get

Returns patient’s “other” document list. Content call is separate.

**/getScannedList?pmsPatientId=941819\&pmsEncounterId=13781624**

\<?xml version="1.0" encoding="utf-16"?\>

\<ScanDocumentReports xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" name="clinical.ScanDocumentReports" conceptType="List"\>

\<group order="dateDescend" referenceID="**07599b84-05c7-4456-ae18-1cc5984a83fc**"\>

\<ScandocumentReport\_SendingFacility conceptID="554:1000111/1000452/2" /\>

\<ScandocumentReport\_Subject conceptID="554:1000111/1000452/3"\>**Audiology \!Referral\&gt;**\</ScandocumentReport\_Subject\>

\<ScandocumentReport\_Name conceptID="554:1000111/1000452/17"\>**Audiology \!Referral\&gt;**\</ScandocumentReport\_Name\>

\<ScandocumentReport\_DateReceived conceptID="554:1000111/1000452/1"\>**2019-02-22T00:00:00**\</ScandocumentReport\_DateReceived\>

\<ScandocumentReport\_DataType conceptID="554:1000111/1000452/15"\>**application/pdf**\</ScandocumentReport\_DataType\>

\<ScandocumentReport\_Comments conceptID="554:1000111/1000452/27" /\>

\<ScanContent /\>

\</group\>

\<group order="dateDescend" referenceID="**dce9b72e-69ec-4293-a77f-d8b419aadc21**"\>

\<ScandocumentReport\_SendingFacility conceptID="554:1000111/1000452/2" /\>

\<ScandocumentReport\_Subject conceptID="554:1000111/1000452/3"\>**Audiology \&amp; Referral**\</ScandocumentReport\_Subject\>

\<ScandocumentReport\_Name conceptID="554:1000111/1000452/17"\>**Audiology \&amp; Referral**\</ScandocumentReport\_Name\>

\<ScandocumentReport\_DateReceived conceptID="554:1000111/1000452/1"\>**2019-02-20T16:55:54**\</ScandocumentReport\_DateReceived\>

\<ScandocumentReport\_DataType conceptID="554:1000111/1000452/15"\>**application/pdf**\</ScandocumentReport\_DataType\>

\<ScandocumentReport\_Comments conceptID="554:1000111/1000452/27" /\>

\<ScanContent /\>

\</group\>

**\[..SNIP...\]**

\<group order="dateDescend" referenceID="**fd03e12b-61c2-48a9-aa58-8c8f36a768fd**"\>

\<ScandocumentReport\_SendingFacility conceptID="554:1000111/1000452/2" /\>

\<ScandocumentReport\_Subject conceptID="554:1000111/1000452/3"\>**Community Nursing Referral**\</ScandocumentReport\_Subject\>

\<ScandocumentReport\_Name conceptID="554:1000111/1000452/17"\>**Community Nursing Referral**\</ScandocumentReport\_Name\>

\<ScandocumentReport\_DateReceived conceptID="554:1000111/1000452/1"\>**2015-05-13T00:00:00**\</ScandocumentReport\_DateReceived\>

\<ScandocumentReport\_DataType conceptID="554:1000111/1000452/15"\>**application/pdf**\</ScandocumentReport\_DataType\>

\<ScandocumentReport\_Comments conceptID="554:1000111/1000452/27" /\>

\<ScanContent /\>

\</group\>

\</ScanDocumentReports\>

## Scanned Document Details

API requirements: GET (read only)

Additional Parameters:

2.  Reference Id – Identifier from Scanned Document Listing concept to ID a single entry

### Get

Returns patient scanned document data.

**/getScannedDetails?pmsPatientId=941819\&pmsEncounterId=13781624\&pmsReferenceId=2229d357-0800-4740-bbc7-f21b03445566**

\<?xml version="1.0" encoding="utf-16"?\>

\<ScanReportContent xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" name="clinical.ScanContent"\>

\<group referenceID="**CC765291-30A3-45DB-91A0-6AE6077415CC**"\>

\<ScandocumentReport\_SendingFacility /\>

\<ScandocumentReport\_Subject /\>

\<ScandocumentReport\_Name /\>

\<ScandocumentReport\_DateReceived /\>

\<ScandocumentReport\_DataType /\>

\<ScandocumentReport\_Comments /\>

\<ScanContent conceptID="554:1000111/1000452/4**"\>/9j/4AAQSkZJRgAB...\[SNIP\]...KACgAoA//2Q==**\</ScanContent\>

\</group\>

\</ScanReportContent\>
