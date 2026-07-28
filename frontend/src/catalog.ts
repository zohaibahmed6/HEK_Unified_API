import type { SystemId } from "./systems";

export type HttpMethod = "GET" | "POST";
export type CallKind = "read" | "write";
export type BodyKind = "none" | "json" | "xml";

export interface ParamDef {
  key: string;
  label: string;
  default: string;
}

export interface EndpointDef {
  id: string;
  system: SystemId;
  name: string;
  method: HttpMethod;
  path: string;
  kind: CallKind;
  /** Extra params this endpoint needs beyond the system's shared auth-context params. */
  extraParams?: ParamDef[];
  bodyKind: BodyKind;
  /** Pre-filled editable body template for POST calls (JSON text or raw XML). */
  bodyTemplate?: (ctx: Record<string, string>) => string;
}

// Shared per-system context params every card can draw on without re-entering them.
export const sharedParams: Record<SystemId, ParamDef[]> = {
  hiso: [{ key: "sessionKey", label: "Session Key", default: "" }],
  karo: [
    { key: "patientId", label: "Patient ID", default: "2459731" },
    { key: "encounterId", label: "Encounter ID", default: "2147488418__901__-__local" },
    { key: "system", label: "System", default: "hss" },
    { key: "pho", label: "PHO", default: "NBPH0" },
  ],
  erms: [
    { key: "pmsPatientId", label: "Patient ID", default: "2459731" },
    { key: "pmsEncounterId", label: "Encounter ID", default: "2147488418__901__-__local" },
  ],
  col: [
    { key: "pmsPatientId", label: "Patient ID", default: "2459731" },
    { key: "pmsEncounterId", label: "Encounter ID", default: "2147488418__901__-__local" },
  ],
};

// Real legacy getData request/section structure, copied field-for-field from a genuine legacy
// response (legacy-reference/referencehisodata.txt) - not invented. Repeating groups omit
// referenceID to list every real DB row (HisoRequestEngine.FillGroupAsync behaviour).
export const FULL_RECORD_REQUEST_XML =
  '<dataContainer>' +
  '<section name="patient.details">' +
  '<field name="nhi" conceptName="Patient_NHI" />' +
  '<field name="dateOfBirth" conceptName="Patient_DateOfBirth" />' +
  '<field name="givenName" conceptName="Patient_FirstName" />' +
  '<field name="secondName" conceptName="Patient_Middlename" />' +
  '<field name="surname" conceptName="Patient_Surname" />' +
  '<field name="previousFamilyName" conceptName="Patient_PreviousSurname" />' +
  '<field name="preferredName" conceptName="Patient_Alias" />' +
  '<field name="gender" conceptName="Patient_Gender" />' +
  '<field name="residentialDHB" conceptName="Patient_ResidentialDHB" />' +
  '</section>' +
  '<section name="patient.contact.details">' +
  '<field name="telephoneDaytime" conceptName="Patient_WorkPhone" />' +
  '<field name="telephoneEvening" conceptName="Patient_ResidentialPhone" />' +
  '<field name="telephoneCell" conceptName="Patient_Mobile" />' +
  '<field name="telephoneAlternative" conceptName="Patient_AlternativeContactNumber" />' +
  '<field name="pmsId" conceptName="Patient_PmsId" />' +
  '<field name="email" conceptName="Patient_Email" />' +
  '</section>' +
  '<section name="patient.residential.address">' +
  '<field name="streetNumber" conceptName="Patient_ResidentialAddress_StreetNumber" />' +
  '<field name="streetAddress" conceptName="Patient_ResidentialAddress_StreetName" />' +
  '<field name="otherDesignation" conceptName="Patient_ResidentialAddress_AdditionalLine" />' +
  '<field name="suburb" conceptName="Patient_ResidentialAddress_Suburb" />' +
  '<field name="city" conceptName="Patient_ResidentialAddress_City" />' +
  '<field name="postCode" conceptName="Patient_ResidentialAddress_Postcode" />' +
  '</section>' +
  '<section name="patient.postal.address">' +
  '<field name="streetNumber" conceptName="Patient_PostalAddress_StreetNumber" />' +
  '<field name="streetAddress" conceptName="Patient_PostalAddress_StreetName" />' +
  '<field name="otherDesignation" conceptName="Patient_PostalAddress_AdditionalLine" />' +
  '<field name="suburb" conceptName="Patient_PostalAddress_Suburb" />' +
  '<field name="city" conceptName="Patient_PostalAddress_City" />' +
  '<field name="postCode" conceptName="Patient_PostalAddress_Postcode" />' +
  '</section>' +
  '<section name="patient.ethnicities">' +
  '<field name="code1Level2" conceptName="Patient_Ethnicity1CodeLevel2" />' +
  '<field name="code2Level2" conceptName="Patient_Ethnicity2CodeLevel2" />' +
  '<field name="code3Level2" conceptName="Patient_Ethnicity3CodeLevel2" />' +
  '</section>' +
  '<section name="patient.disabilities">' +
  '<field name="communicationImpaired" conceptName="Patient_IsCommunicationImpaired" />' +
  '<field name="visionImpaired" conceptName="Patient_IsVisionImpaired" />' +
  '<field name="intellectuallyImpaired" conceptName="Patient_IsIntellectuallyImpaired" />' +
  '<field name="mobilityStatus" conceptName="Patient_IsMobilityImpaired" />' +
  '</section>' +
  '<section name="patient.language">' +
  '<field name="requiresInterpreter" conceptName="Patient_IsInterpreterRequired" />' +
  '<field name="preferredLanguage" conceptName="Patient_PreferredLanguage" />' +
  '</section>' +
  '<section name="administrative.eligibility.details">' +
  '<field name="status" conceptName="Patient_IsEligiblePublicFunds" />' +
  '<field name="idType" conceptName="Patient_IDType" />' +
  '<field name="idNumber" conceptName="Patient_IDNumber" />' +
  '<field name="countryOfBirth" conceptName="Patient_CountryOfBirth" />' +
  '<field name="dateOfEntry" conceptName="Patient_DateOfEntryNZ" />' +
  '</section>' +
  '<section name="scannedDocuments">' +
  '<group name="scannedDocument" conceptName="Patient_Attachment">' +
  '<field name="date" conceptName="Patient_Attachment_DateCreated" />' +
  '<field name="name" conceptName="Patient_Attachment_Name" />' +
  '<field name="size" conceptName="Patient_Attachment_Size" />' +
  '<field name="comments" conceptName="Patient_Attachment_Comments" />' +
  '<field name="author" conceptName="Patient_Attachment_Author" />' +
  '<field name="type" conceptName="Patient_Attachment_DataType" />' +
  '<field name="id" conceptName="Patient_Attachment_ID" />' +
  '<field name="subject" conceptName="Patient_Attachment_Subject" />' +
  '<field name="documentType" conceptName="Patient_Attachment_Type" />' +
  '</group>' +
  '<group name="scannedDocument" conceptName="Patient_IncomingLetter">' +
  '<field name="date" conceptName="Patient_IncomingLetter_DateCreated" />' +
  '<field name="name" conceptName="Patient_IncomingLetter_Name" />' +
  '<field name="size" conceptName="Patient_IncomingLetter_Size" />' +
  '<field name="comments" conceptName="Patient_IncomingLetter_Comments" />' +
  '<field name="author" conceptName="Patient_IncomingLetter_Author" />' +
  '<field name="type" conceptName="Patient_IncomingLetter_DataType" />' +
  '<field name="id" conceptName="Patient_IncomingLetter_ID" />' +
  '<field name="subject" conceptName="Patient_IncomingLetter_Subject" />' +
  '<field name="documentType" conceptName="Patient_IncomingLetter_Type" />' +
  '</group>' +
  '<group name="scannedDocument" conceptName="Patient_OutgoingLetter">' +
  '<field name="date" conceptName="Patient_OutgoingLetter_DateCreated" />' +
  '<field name="name" conceptName="Patient_OutgoingLetter_Name" />' +
  '<field name="size" conceptName="Patient_OutgoingLetter_Size" />' +
  '<field name="comments" conceptName="Patient_OutgoingLetter_Comments" />' +
  '<field name="author" conceptName="Patient_OutgoingLetter_Author" />' +
  '<field name="type" conceptName="Patient_OutgoingLetter_DataType" />' +
  '<field name="id" conceptName="Patient_OutgoingLetter_ID" />' +
  '<field name="subject" conceptName="Patient_OutgoingLetter_Subject" />' +
  '<field name="documentType" conceptName="Patient_OutgoingLetter_Type" />' +
  '</group>' +
  '</section>' +
  '<section name="clinical.diagnosticReports">' +
  '<group name="clinical.diagnosticReport" conceptName="Patient_LaboratoryReport">' +
  '<field name="sendingFacility" conceptName="Patient_LaboratoryReport_SendingFacility" />' +
  '<field name="name" conceptName="Patient_LaboratoryReport_Subject" />' +
  '<field name="date" conceptName="Patient_LaboratoryReport_DateReceived" />' +
  '<field name="type" conceptName="Patient_LaboratoryReport_DataType" />' +
  '<field name="comments" conceptName="Patient_LaboratoryReport_Comments" />' +
  '<field name="size" conceptName="Patient_LaboratoryReport_Size" />' +
  '<field name="documentType" conceptName="Patient_LaboratoryReport_Type" />' +
  '</group>' +
  '<group name="clinical.diagnosticReport" conceptName="Patient_RadiologyReport">' +
  '<field name="sendingFacility" conceptName="Patient_RadiologyReport_SendingFacility" />' +
  '<field name="name" conceptName="Patient_RadiologyReport_Subject" />' +
  '<field name="date" conceptName="Patient_RadiologyReport_DateReceived" />' +
  '<field name="type" conceptName="Patient_RadiologyReport_DataType" />' +
  '<field name="comments" conceptName="Patient_RadiologyReport_Comments" />' +
  '<field name="size" conceptName="Patient_RadiologyReport_Size" />' +
  '<field name="documentType" conceptName="Patient_RadiologyReport_Type" />' +
  '</group>' +
  '</section>' +
  '<section name="clinical.medicalHistory">' +
  '<group name="clinical.familyHistory" conceptName="Patient_FamilyHistory">' +
  '<field name="dateRecorded" conceptName="Patient_FamilyHistory_DateRecorded" />' +
  '<field name="problemCode" conceptName="Patient_FamilyHistory_ProblemCode" />' +
  '<field name="problemDescription" conceptName="Patient_FamilyHistory_ProblemDescription" />' +
  '<field name="comments" conceptName="Patient_FamilyHistory_Comments" />' +
  '</group>' +
  '<group name="clinical.smoking" conceptName="Patient_Smoking">' +
  '<field name="description" conceptName="Patient_Smoking_ConsumptionDescription" />' +
  '<field name="code" conceptName="Patient_Smoking_Code" />' +
  '<field name="codingSystem" conceptName="Patient_Smoking_CodingSystem" />' +
  '</group>' +
  '<group name="clinical.patientHistory" conceptName="Patient_SocialHistory">' +
  '<field name="dateRecorded" conceptName="Patient_SocialHistory_DateRecorded" />' +
  '<field name="description" conceptName="Patient_SocialHistory_Description" />' +
  '<field name="code" conceptName="Patient_SocialHistory_Code" />' +
  '<field name="codingSystem" conceptName="Patient_SocialHistory_CodingSystem" />' +
  '</group>' +
  '<group name="clinical.patientHistory" conceptName="Patient_PastHistory">' +
  '<field name="dateRecorded" conceptName="Patient_PastHistory_DateRecorded" />' +
  '<field name="description" conceptName="Patient_PastHistory_Description" />' +
  '<field name="code" conceptName="Patient_PastHistory_Code" />' +
  '<field name="codingSystem" conceptName="Patient_PastHistory_CodingSystem" />' +
  '<field name="comments" conceptName="Patient_PastHistory_Comments" />' +
  '</group>' +
  '<group name="clinical.coMorbidity" conceptName="Patient_Problem">' +
  '<field name="dateRecorded" conceptName="Patient_Problem_DateRecorded" />' +
  '<field name="dateOfOnset" conceptName="Patient_Problem_DateOfOnset" />' +
  '<field name="readCode" conceptName="Patient_Problem_Code" />' +
  '<field name="description" conceptName="Patient_Problem_Description" />' +
  '<field name="comments" conceptName="Patient_Problem_Comments" />' +
  '</group>' +
  '</section>' +
  '<section name="clinical.medications">' +
  '<group name="clinical.medication.longterm" conceptName="Patient_RegularMedication">' +
  '<field name="startDate" conceptName="Patient_RegularMedication_StartedDate" />' +
  '<field name="medicationFullName" conceptName="Patient_RegularMedication_Name" />' +
  '<field name="medicationCode" conceptName="Patient_RegularMedication_Code" />' +
  '<field name="medicationCodingSystem" conceptName="Patient_RegularMedication_CodingSystem" />' +
  '<field name="dose" conceptName="Patient_RegularMedication_DosageQuantity" />' +
  '<field name="unitOfMeasure" conceptName="Patient_RegularMedication_DosageUnit" />' +
  '<field name="administrationInstructions" conceptName="Patient_RegularMedication_Administrationinstructions" />' +
  '<field name="lastPrescribedDate" conceptName="Patient_RegularMedication_LastPrescribedDate" />' +
  '</group>' +
  '<group name="clinical.medicalWarning" conceptName="Patient_MedicalWarning">' +
  '<field name="date" conceptName="Patient_MedicalWarning_Date" />' +
  '<field name="description" conceptName="Patient_MedicalWarning_Description" />' +
  '<field name="comments" conceptName="Patient_MedicalWarning_Comments" />' +
  '</group>' +
  '<group name="clinical.medicalWarning" conceptName="Patient_Allergy">' +
  '<field name="date" conceptName="Patient_Allergy_Date" />' +
  '<field name="description" conceptName="Patient_Allergy_AllergenDescription" />' +
  '<field name="comments" conceptName="Patient_Allergy_ReactionDescription" />' +
  '</group>' +
  '</section>' +
  '<section name="measurements">' +
  '<field name="bloodPressureSystolic" conceptName="Patient_Measurement" qualifierID="271649006" />' +
  '<field name="bloodPressureDiastolic" conceptName="Patient_Measurement" qualifierID="271650006" />' +
  '<field name="bloodPressure" conceptName="Patient_Measurement" qualifierID="75367002" />' +
  '<field name="weight" conceptName="Patient_Measurement" qualifierID="27113001" />' +
  '<field name="height" conceptName="Patient_Measurement" qualifierID="50373000" />' +
  '<field name="bmi" conceptName="Patient_Measurement" qualifierID="60621009" />' +
  '<field name="cholesterolTotal" conceptName="Patient_TestResult" qualifierID="14647-2" />' +
  '<field name="cholesterolHdl" conceptName="Patient_TestResult" qualifierID="14646-4" />' +
  '<field name="cholesterolTriglyceride" conceptName="Patient_TestResult" qualifierID="14927-8" />' +
  '<field name="cholesterolLdl" conceptName="Patient_TestResult" qualifierID="39469-2" />' +
  '<field name="serumCreatinine" conceptName="Patient_TestResult" qualifierID="14682-9" />' +
  '<field name="egfr" conceptName="Patient_TestResult" qualifierID="33914-3" />' +
  '<field name="hbA1cDcct" conceptName="Patient_TestResult" qualifierID="4548-4" />' +
  '<field name="hbA1c" conceptName="Patient_TestResult" qualifierID="59261-8" />' +
  '<field name="diabetesType" conceptName="Patient_Condition_Exists" qualifierID="73211009" />' +
  '</section>' +
  '<section name="clinical.general">' +
  '<field name="urgent" conceptName="Patient_Referral_Urgency" />' +
  '<field name="urgentDetails" conceptName="Patient_Referral_UrgencySupportingInformation" />' +
  '<group name="clinical.acc" conceptName="Patient_Accident">' +
  '<field name="acc45Number" conceptName="Patient_Accident_RegistrationNumber" />' +
  '<field name="accidentDate" conceptName="Patient_Accident_Date" />' +
  '<field name="accidentDescription" conceptName="Patient_Accident_DiagnosisDescription" />' +
  '<field name="isWorkRelated" conceptName="Patient_Accident_IsWorkRelated" />' +
  '<field name="accidentLocation" conceptName="Patient_Accident_Location" />' +
  '</group>' +
  '</section>' +
  '<section name="administrative">' +
  '<field name="referralCreationDateTime" conceptName="Patient_Referral_Date" />' +
  '<field name="referredToServiceCode" conceptName="Patient_Referral_ReferredSpeciality" />' +
  '<field name="referredFor" conceptName="Patient_Referral_ReferredService" />' +
  '<field name="pmsApplication" conceptName="PMS_ApplicationName" />' +
  '<field name="pmsManufacturer" conceptName="PMS_ApplicationManufacturer" />' +
  '<field name="dictionaryVersion" conceptName="PMS_SupportedConceptVersion" />' +
  '<field name="pmsApplicationVersion" conceptName="PMS_Application Version" />' +
  '</section>' +
  '<section name="recipient">' +
  '<field name="pmsId" conceptName="TargetPractitioner_PMSID" />' +
  '<field name="organisationEdi" conceptName="TargetOrganisation_HealthlinkEDI" />' +
  '<field name="organisationAlias" conceptName="TargetOrganisation_Alias" />' +
  '<field name="organisationAliasIssuer" conceptName="TargetOrganisation_AliasIssuer" />' +
  '</section>' +
  '<section name="referrer.details">' +
  '<field name="fullName" conceptName="CurrentUser_FullName" />' +
  '<field name="givenName" conceptName="CurrentUser_FirstName" />' +
  '<field name="surname" conceptName="CurrentUser_Surname" />' +
  '<field name="hpi" conceptName="CurrentUser_PersonalHPI" />' +
  '<field name="pmsId" conceptName="CurrentUser_PMSID" />' +
  '<field name="referrerEdi" conceptName="CurrentUserOrganisation_HealthlinkEDI" />' +
  '<field name="workPhone" conceptName="CurrentUser_WorkPhone" />' +
  '<field name="organisationName" conceptName="CurrentUserOrganisation_Name" />' +
  '<field name="organisationFacilityHpi" conceptName="CurrentUserOrganisation_FacilityHPI" />' +
  '<field name="organisationFax" conceptName="CurrentUserOrganisation_Fax" />' +
  '</section>' +
  "</dataContainer>";

const dateRangeParams: ParamDef[] = [
  { key: "pmsOrder", label: "Order", default: "" },
  { key: "pmsMinDateTime", label: "Min DateTime", default: "" },
  { key: "pmsMaxDateTime", label: "Max DateTime", default: "" },
];

export const endpoints: EndpointDef[] = [
  // ---- HISO (JSON-compat; real SOAP facade mirrors these 1:1 at /FormSessionService.svc) ----
  {
    id: "hiso-getVersion",
    system: "hiso",
    name: "getVersion",
    method: "POST",
    path: "/hiso/getVersion",
    kind: "read",
    bodyKind: "json",
    bodyTemplate: (ctx) => JSON.stringify({ sessionKey: ctx.sessionKey }, null, 2),
  },
  {
    id: "hiso-getDeliveryOptions",
    system: "hiso",
    name: "getDeliveryOptions",
    method: "POST",
    path: "/hiso/getDeliveryOptions",
    kind: "read",
    bodyKind: "json",
    bodyTemplate: (ctx) => JSON.stringify({ sessionKey: ctx.sessionKey }, null, 2),
  },
  {
    id: "hiso-getFormView",
    system: "hiso",
    name: "getFormView",
    method: "POST",
    path: "/hiso/getFormView",
    kind: "read",
    bodyKind: "json",
    bodyTemplate: (ctx) => JSON.stringify({ sessionKey: ctx.sessionKey }, null, 2),
  },
  {
    id: "hiso-getData-record",
    system: "hiso",
    name: "getData — full patient record",
    method: "POST",
    path: "/hiso/getData",
    kind: "read",
    bodyKind: "json",
    // submittedDataXml mirrors the REAL legacy getData structure 1:1, copied field-for-field from
    // legacy-reference/referencehisodata.txt (a genuine legacy response) - no invented sections/fields.
    // Repeating groups (Patient_Attachment, Patient_Accident, Patient_PastHistory, etc.) are sent with
    // no referenceID so HisoRequestEngine.FillGroupAsync returns one cloned group per real DB row.
    bodyTemplate: (ctx) =>
      JSON.stringify(
        {
          sessionKey: ctx.sessionKey,
          dataContainer: {
            formMetaData: { formInstanceOperationMode: "N" },
            submittedDataXml: FULL_RECORD_REQUEST_XML,
          },
        },
        null,
        2,
      ),
  },
  {
    id: "hiso-processAction",
    system: "hiso",
    name: "processAction (write)",
    method: "POST",
    path: "/hiso/processAction",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "actionId", label: "Action ID", default: "addTask" },
      { key: "actionContainerXml", label: "Action Container XML", default: "<actionContainer></actionContainer>" },
    ],
    bodyTemplate: (ctx) => JSON.stringify({ sessionKey: ctx.sessionKey, actionId: ctx.actionId, actionContainerXml: ctx.actionContainerXml }, null, 2),
  },
  {
    id: "hiso-saveContainer",
    system: "hiso",
    name: "saveContainer (write)",
    method: "POST",
    path: "/hiso/saveContainer",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "resumePath", label: "Resume Path", default: "" },
      { key: "view", label: "View", default: "" },
      { key: "viewType", label: "View Type", default: "" },
      { key: "submittedDataXml", label: "Submitted Data XML", default: "<dataContainer></dataContainer>" },
    ],
    bodyTemplate: (ctx) =>
      JSON.stringify(
        {
          sessionKey: ctx.sessionKey,
          formMetaData: { formInstanceOperationMode: "N" },
          resumePath: ctx.resumePath,
          view: ctx.view,
          viewType: ctx.viewType,
          completed: false,
          submittedDataXml: ctx.submittedDataXml,
        },
        null,
        2,
      ),
  },

  // ---- KARO ----
  { id: "karo-ping", system: "karo", name: "ping", method: "GET", path: "/karo/ping", kind: "read", bodyKind: "none" },
  { id: "karo-demographics", system: "karo", name: "demographics", method: "GET", path: "/karo/demographics", kind: "read", bodyKind: "none" },
  { id: "karo-clinicalnotes-get", system: "karo", name: "clinicalnotes (list)", method: "GET", path: "/karo/clinicalnotes", kind: "read", bodyKind: "none" },
  { id: "karo-conditions-get", system: "karo", name: "conditions (list)", method: "GET", path: "/karo/conditions", kind: "read", bodyKind: "none" },
  {
    id: "karo-documents",
    system: "karo",
    name: "documents",
    method: "GET",
    path: "/karo/documents",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "identifier", label: "Identifier", default: "" }],
  },
  { id: "karo-labresults", system: "karo", name: "labresults", method: "GET", path: "/karo/labresults", kind: "read", bodyKind: "none" },
  { id: "karo-medications", system: "karo", name: "medications", method: "GET", path: "/karo/medications", kind: "read", bodyKind: "none" },
  {
    id: "karo-observations-get",
    system: "karo",
    name: "observations (list)",
    method: "GET",
    path: "/karo/observations",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "conceptId", label: "Concept ID", default: "" }],
  },
  {
    id: "karo-provider",
    system: "karo",
    name: "provider",
    method: "GET",
    path: "/karo/provider",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "userId", label: "User ID", default: "" }],
  },
  {
    id: "karo-recallcategories",
    system: "karo",
    name: "recallcategories",
    method: "GET",
    path: "/karo/recallcategories",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "group", label: "Group", default: "" }],
  },
  {
    id: "karo-encountersummary",
    system: "karo",
    name: "encountersummary",
    method: "GET",
    path: "/karo/encountersummary",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "identifier", label: "Identifier", default: "" }],
  },
  { id: "karo-recalls-get", system: "karo", name: "recalls (list)", method: "GET", path: "/karo/recalls", kind: "read", bodyKind: "none" },
  { id: "karo-screeningcodes-get", system: "karo", name: "screeningcodes (list)", method: "GET", path: "/karo/screeningcodes", kind: "read", bodyKind: "none" },
  {
    id: "karo-patientattachment",
    system: "karo",
    name: "patientattachment",
    method: "GET",
    path: "/karo/patientattachment",
    kind: "read",
    bodyKind: "none",
    extraParams: [
      { key: "referenceID", label: "Reference ID", default: "" },
      { key: "sortOrder", label: "Sort Order", default: "" },
      { key: "subject", label: "Subject", default: "" },
      { key: "dateFrom", label: "Date From", default: "" },
      { key: "dateTo", label: "Date To", default: "" },
    ],
  },
  {
    id: "karo-clinicalnotes-post",
    system: "karo",
    name: "clinicalnotes (write)",
    method: "POST",
    path: "/karo/clinicalnotes",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "userId", label: "User ID", default: "1" },
      { key: "subjectiveNotes", label: "Subjective Notes", default: "" },
      { key: "objectiveNotes", label: "Objective Notes", default: "" },
      { key: "assessment", label: "Assessment", default: "" },
      { key: "plans", label: "Plans", default: "" },
    ],
    bodyTemplate: (ctx) =>
      JSON.stringify(
        { patientId: ctx.patientId, encounterId: ctx.encounterId, userId: ctx.userId, subjectiveNotes: ctx.subjectiveNotes, objectiveNotes: ctx.objectiveNotes, assessment: ctx.assessment, plans: ctx.plans },
        null,
        2,
      ),
  },
  {
    id: "karo-conditions-post",
    system: "karo",
    name: "conditions (write)",
    method: "POST",
    path: "/karo/conditions",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "userId", label: "User ID", default: "1" },
      { key: "type", label: "Type", default: "" },
      { key: "onSetDate", label: "Onset Date", default: "" },
      { key: "summary", label: "Summary", default: "" },
      { key: "isLongTerm", label: "Is Long Term (true/false)", default: "false" },
      { key: "conceptId", label: "Concept ID", default: "" },
      { key: "name", label: "Name", default: "" },
      { key: "fsn", label: "FSN", default: "" },
    ],
    bodyTemplate: (ctx) =>
      JSON.stringify(
        {
          patientId: ctx.patientId,
          encounterId: ctx.encounterId,
          userId: ctx.userId,
          type: ctx.type,
          onSetDate: ctx.onSetDate,
          summary: ctx.summary,
          isLongTerm: ctx.isLongTerm,
          conceptId: ctx.conceptId,
          name: ctx.name,
          fsn: ctx.fsn,
        },
        null,
        2,
      ),
  },
  {
    id: "karo-invoice-post",
    system: "karo",
    name: "invoice (write)",
    method: "POST",
    path: "/karo/invoice",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "userId", label: "User ID", default: "1" },
      { key: "name", label: "Name", default: "" },
      { key: "code", label: "Code", default: "" },
      { key: "fee", label: "Fee", default: "0" },
      { key: "payee", label: "Payee", default: "" },
    ],
    bodyTemplate: (ctx) =>
      JSON.stringify({ patientId: ctx.patientId, encounterId: ctx.encounterId, userId: ctx.userId, name: ctx.name, code: ctx.code, fee: ctx.fee, payee: ctx.payee }, null, 2),
  },
  {
    id: "karo-observations-post",
    system: "karo",
    name: "observations (write)",
    method: "POST",
    path: "/karo/observations",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "userId", label: "User ID", default: "1" },
      { key: "temperature", label: "Temperature", default: "0" },
      { key: "waistCircumference", label: "Waist Circumference", default: "0" },
      { key: "height", label: "Height", default: "0" },
      { key: "weight", label: "Weight", default: "0" },
      { key: "bpSys", label: "BP Systolic", default: "0" },
      { key: "bpDia", label: "BP Diastolic", default: "0" },
      { key: "heartRate", label: "Heart Rate", default: "0" },
      { key: "notes", label: "Notes", default: "" },
    ],
    bodyTemplate: (ctx) =>
      JSON.stringify(
        {
          patientId: ctx.patientId,
          encounterId: ctx.encounterId,
          userId: ctx.userId,
          temperature: ctx.temperature,
          waistCircumference: ctx.waistCircumference,
          height: ctx.height,
          weight: ctx.weight,
          bpSys: ctx.bpSys,
          bpDia: ctx.bpDia,
          heartRate: ctx.heartRate,
          notes: ctx.notes,
        },
        null,
        2,
      ),
  },
  {
    id: "karo-recalls-post",
    system: "karo",
    name: "recalls (write)",
    method: "POST",
    path: "/karo/recalls",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "userId", label: "User ID", default: "1" },
      { key: "priority", label: "Priority", default: "" },
      { key: "group", label: "Group", default: "" },
      { key: "dueDate", label: "Due Date", default: "" },
      { key: "notes", label: "Notes", default: "" },
      { key: "categoryId", label: "Category ID", default: "" },
    ],
    bodyTemplate: (ctx) =>
      JSON.stringify(
        { patientId: ctx.patientId, encounterId: ctx.encounterId, userId: ctx.userId, priority: ctx.priority, group: ctx.group, dueDate: ctx.dueDate, notes: ctx.notes, categoryId: ctx.categoryId },
        null,
        2,
      ),
  },
  {
    id: "karo-document-post",
    system: "karo",
    name: "document (write)",
    method: "POST",
    path: "/karo/document",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "messageData", label: "Message Data (base64)", default: "" },
      { key: "contentType", label: "Content Type", default: "text/plain" },
      { key: "messageSubject", label: "Message Subject", default: "" },
      { key: "itemType", label: "Item Type", default: "" },
    ],
    bodyTemplate: (ctx) =>
      JSON.stringify({ patientId: ctx.patientId, encounterId: ctx.encounterId, messageData: ctx.messageData, contentType: ctx.contentType, messageSubject: ctx.messageSubject, itemType: ctx.itemType }, null, 2),
  },
  {
    id: "karo-summary-post",
    system: "karo",
    name: "summary (write)",
    method: "POST",
    path: "/karo/summary",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "identifier", label: "Identifier", default: "" },
      { key: "providerID", label: "Provider ID", default: "" },
      { key: "dateTimeRecorded", label: "Date/Time Recorded", default: "" },
    ],
    bodyTemplate: (ctx) =>
      JSON.stringify({ patientId: ctx.patientId, encounterID: ctx.encounterId, system: ctx.system, identifier: ctx.identifier, providerID: ctx.providerID, dateTimeRecorded: ctx.dateTimeRecorded, entry: [] }, null, 2),
  },

  // ---- ERMS ----
  { id: "erms-ping", system: "erms", name: "ping", method: "GET", path: "/erms/ping", kind: "read", bodyKind: "none" },
  { id: "erms-GetPatientData", system: "erms", name: "GetPatientData", method: "GET", path: "/erms/GetPatientData", kind: "read", bodyKind: "none" },
  { id: "erms-GetPatientMeasurement", system: "erms", name: "GetPatientMeasurement", method: "GET", path: "/erms/GetPatientMeasurement", kind: "read", bodyKind: "none" },
  { id: "erms-GetSmokingStatus", system: "erms", name: "GetSmokingStatus", method: "GET", path: "/erms/GetSmokingStatus", kind: "read", bodyKind: "none" },
  {
    id: "erms-GetCurrentUser",
    system: "erms",
    name: "GetCurrentUser",
    method: "GET",
    path: "/erms/GetCurrentUser",
    kind: "read",
    bodyKind: "none",
    extraParams: [
      { key: "LocationId", label: "Location ID", default: "" },
      { key: "pmsUserId", label: "User ID", default: "" },
    ],
  },
  { id: "erms-GetNextOfKin", system: "erms", name: "GetNextOfKin", method: "GET", path: "/erms/GetNextOfKin", kind: "read", bodyKind: "none" },
  {
    id: "erms-GetRegisteredPractitioners",
    system: "erms",
    name: "GetRegisteredPractitioners",
    method: "GET",
    path: "/erms/GetRegisteredPractitioners",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "pmsLocationId", label: "Location ID", default: "" }],
  },
  { id: "erms-GetAccidents", system: "erms", name: "GetAccidents", method: "GET", path: "/erms/GetAccidents", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  { id: "erms-GetClassifications", system: "erms", name: "GetClassifications", method: "GET", path: "/erms/GetClassifications", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  { id: "erms-GetConsultNotes", system: "erms", name: "GetConsultNotes", method: "GET", path: "/erms/GetConsultNotes", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  { id: "erms-GetMedicalAllergies", system: "erms", name: "GetMedicalAllergies", method: "GET", path: "/erms/GetMedicalAllergies", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  { id: "erms-GetPrescribedMedications", system: "erms", name: "GetPrescribedMedications", method: "GET", path: "/erms/GetPrescribedMedications", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  { id: "erms-GetRegularMedications", system: "erms", name: "GetRegularMedications", method: "GET", path: "/erms/GetRegularMedications", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  { id: "erms-GetLaboratoryReportList", system: "erms", name: "GetLaboratoryReportList", method: "GET", path: "/erms/GetLaboratoryReportList", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  { id: "erms-GetRadiologyReportList", system: "erms", name: "GetRadiologyReportList", method: "GET", path: "/erms/GetRadiologyReportList", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  {
    id: "erms-GetDischargeSummaryReportList",
    system: "erms",
    name: "GetDischargeSummaryReportList",
    method: "GET",
    path: "/erms/GetDischargeSummaryReportList",
    kind: "read",
    bodyKind: "none",
    extraParams: dateRangeParams,
  },
  { id: "erms-GetScannedList", system: "erms", name: "GetScannedList", method: "GET", path: "/erms/GetScannedList", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  {
    id: "erms-GetLaboratoryReportDetails",
    system: "erms",
    name: "GetLaboratoryReportDetails",
    method: "GET",
    path: "/erms/GetLaboratoryReportDetails",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "pmsReferenceId", label: "Reference ID", default: "" }],
  },
  {
    id: "erms-GetRadiologyReportDetails",
    system: "erms",
    name: "GetRadiologyReportDetails",
    method: "GET",
    path: "/erms/GetRadiologyReportDetails",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "pmsReferenceId", label: "Reference ID", default: "" }],
  },
  {
    id: "erms-GetDischargeSummaryDetails",
    system: "erms",
    name: "GetDischargeSummaryDetails",
    method: "GET",
    path: "/erms/GetDischargeSummaryDetails",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "pmsReferenceId", label: "Reference ID", default: "" }],
  },
  {
    id: "erms-GetScannedDetails",
    system: "erms",
    name: "GetScannedDetails",
    method: "GET",
    path: "/erms/GetScannedDetails",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "pmsReferenceId", label: "Reference ID", default: "" }],
  },
  {
    id: "erms-SaveDocument",
    system: "erms",
    name: "SaveDocument (write)",
    method: "POST",
    path: "/erms/SaveDocument",
    kind: "write",
    bodyKind: "xml",
    extraParams: [
      { key: "docId", label: "Document ID", default: "" },
      { key: "providerId", label: "Provider ID", default: "" },
      { key: "docType", label: "Type", default: "" },
      { key: "itemType", label: "Item Type", default: "" },
      { key: "createdDate", label: "Created Date", default: "" },
      { key: "contentType", label: "Content Type", default: "text/plain" },
      { key: "content", label: "Content (plain text - base64-encoded automatically)", default: "" },
    ],
    // Backend does Convert.FromBase64String(Content) (ErmsSaveDocumentCommand.cs) - encode the
    // plain text the user types so they don't have to hand-produce base64.
    // Element names must match ReferralDocument's real [XmlElement] names (ErmsReferralDocument.cs) -
    // plain tag names like <EncounterID> are silently ignored by XmlSerializer (no error, just null
    // fields), which was quietly breaking routing/patientId/encounterId on every real call.
    bodyTemplate: (ctx) =>
      `<ReferralDocument><ReferralDocument_Referral_ID></ReferralDocument_Referral_ID><ReferralDocument_Document_ID>${ctx.docId}</ReferralDocument_Document_ID><ReferralDocument_Patient_PMS_ID>${ctx.pmsPatientId}</ReferralDocument_Patient_PMS_ID><ReferralDocument_Encounter_ID>${ctx.pmsEncounterId}</ReferralDocument_Encounter_ID><ReferralDocument_Referrer_PMS_ID>${ctx.providerId}</ReferralDocument_Referrer_PMS_ID><ReferralDocument_Referral_Type>${ctx.docType}</ReferralDocument_Referral_Type><ReferralDocument_Item_Type>${ctx.itemType}</ReferralDocument_Item_Type><ReferralDocument_Created_Date>${ctx.createdDate}</ReferralDocument_Created_Date><ReferralDocument_Content_Type>${ctx.contentType}</ReferralDocument_Content_Type><ReferralDocument_Content>${ctx.content ? btoa(ctx.content) : ""}</ReferralDocument_Content></ReferralDocument>`,
  },

  // ---- COL ----
  { id: "col-GetCurrentPatientData", system: "col", name: "GetCurrentPatientData", method: "GET", path: "/erms/col/GetCurrentPatientData", kind: "read", bodyKind: "none" },
  { id: "col-GetSessionData", system: "col", name: "GetSessionData", method: "GET", path: "/erms/col/GetSessionData", kind: "read", bodyKind: "none" },
  { id: "col-GetProviderData", system: "col", name: "GetProviderData", method: "GET", path: "/erms/col/GetProviderData", kind: "read", bodyKind: "none" },
  {
    id: "col-GetSurgeryData",
    system: "col",
    name: "GetSurgeryData",
    method: "GET",
    path: "/erms/col/GetSurgeryData",
    kind: "read",
    bodyKind: "none",
    extraParams: [{ key: "LocationId", label: "Location ID", default: "" }],
  },
  { id: "col-GetDiagnosisData", system: "col", name: "GetDiagnosisData", method: "GET", path: "/erms/col/GetDiagnosisData", kind: "read", bodyKind: "none", extraParams: dateRangeParams },
  {
    id: "col-SaveInvoice",
    system: "col",
    name: "SaveInvoice (write)",
    method: "POST",
    path: "/erms/col/SaveInvoice",
    kind: "write",
    bodyKind: "json",
    extraParams: [
      { key: "serviceName", label: "Service Name", default: "" },
      { key: "serviceCode", label: "Service Code", default: "" },
      { key: "amountInclGst", label: "Amount (incl. GST)", default: "0" },
      { key: "description", label: "Description", default: "" },
      { key: "payee", label: "Payee", default: "" },
      { key: "serviceProvider", label: "Service Provider", default: "" },
      { key: "serviceProviderType", label: "Service Provider Type", default: "" },
      { key: "serviceDate", label: "Service Date", default: "" },
      { key: "pegasusReference", label: "Pegasus Reference", default: "" },
      { key: "claimShortCode", label: "Claim Short Code", default: "" },
    ],
    bodyTemplate: (ctx) =>
      JSON.stringify(
        {
          PatientID: ctx.pmsPatientId,
          AccountHolderID: ctx.pmsPatientId,
          EncounterID: ctx.pmsEncounterId,
          ServiceName: ctx.serviceName,
          ServiceCode: ctx.serviceCode,
          AmountInclGST: ctx.amountInclGst,
          Description: ctx.description,
          Payee: ctx.payee,
          ServiceProvider: ctx.serviceProvider,
          ServiceProviderType: ctx.serviceProviderType,
          ServiceDate: ctx.serviceDate,
          PegasusReference: ctx.pegasusReference,
          ClaimShortCode: ctx.claimShortCode,
        },
        null,
        2,
      ),
  },
];

export function endpointsForSystem(system: SystemId): EndpointDef[] {
  return endpoints.filter((e) => e.system === system);
}
