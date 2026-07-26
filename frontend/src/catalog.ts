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
    { key: "encounterId", label: "Encounter ID", default: "2147488418__901__FZZ999-B" },
    { key: "system", label: "System", default: "hss" },
    { key: "pho", label: "PHO", default: "NBPH0" },
  ],
  erms: [
    { key: "pmsPatientId", label: "Patient ID", default: "2459731" },
    { key: "pmsEncounterId", label: "Encounter ID", default: "2147488418__901__FZZ999-B" },
  ],
  col: [
    { key: "pmsPatientId", label: "Patient ID", default: "2459731" },
    { key: "pmsEncounterId", label: "Encounter ID", default: "2147488418__901__FZZ999-B" },
  ],
};

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
    id: "hiso-getData-demographics",
    system: "hiso",
    name: "getData — demographics",
    method: "POST",
    path: "/hiso/getData",
    kind: "read",
    bodyKind: "json",
    bodyTemplate: (ctx) =>
      JSON.stringify(
        {
          sessionKey: ctx.sessionKey,
          dataContainer: {
            formMetaData: { formInstanceOperationMode: "N" },
            submittedDataXml:
              '<dataContainer><section name="demographics">' +
              '<field name="firstName" conceptName="Patient_FirstName" />' +
              '<field name="lastName" conceptName="Patient_Surname" />' +
              '<field name="dateOfBirth" conceptName="Patient_DateOfBirth" />' +
              "</section></dataContainer>",
          },
        },
        null,
        2,
      ),
  },
  {
    id: "hiso-getData-attachment",
    system: "hiso",
    name: "getData — Patient_Attachment (AWS content)",
    method: "POST",
    path: "/hiso/getData",
    kind: "read",
    bodyKind: "json",
    bodyTemplate: (ctx) =>
      JSON.stringify(
        {
          sessionKey: ctx.sessionKey,
          dataContainer: {
            formMetaData: { formInstanceOperationMode: "N" },
            submittedDataXml:
              '<dataContainer><section name="documents">' +
              '<group name="attachment" conceptName="Patient_Attachment" referenceID="324ff93e-746c-420b-9bf7-1c1bfda966cd">' +
              '<field name="filename" conceptName="Patient_Attachment_Filename" />' +
              '<field name="size" conceptName="Patient_Attachment_Size" />' +
              '<field name="dataType" conceptName="Patient_Attachment_DataType" />' +
              '<field name="content" conceptName="Patient_Attachment_Content" />' +
              "</group></section></dataContainer>",
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
        { patientId: ctx.patientId, encounterId: ctx.encounterId, userId: Number(ctx.userId) || ctx.userId, subjectiveNotes: ctx.subjectiveNotes, objectiveNotes: ctx.objectiveNotes, assessment: ctx.assessment, plans: ctx.plans },
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
          userId: Number(ctx.userId) || ctx.userId,
          type: ctx.type,
          onSetDate: ctx.onSetDate,
          summary: ctx.summary,
          isLongTerm: ctx.isLongTerm === "true",
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
      JSON.stringify({ patientId: ctx.patientId, encounterId: ctx.encounterId, userId: Number(ctx.userId) || ctx.userId, name: ctx.name, code: ctx.code, fee: Number(ctx.fee) || 0, payee: ctx.payee }, null, 2),
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
          userId: Number(ctx.userId) || ctx.userId,
          temperature: Number(ctx.temperature) || 0,
          waistCircumference: Number(ctx.waistCircumference) || 0,
          height: Number(ctx.height) || 0,
          weight: Number(ctx.weight) || 0,
          bpSys: Number(ctx.bpSys) || 0,
          bpDia: Number(ctx.bpDia) || 0,
          heartRate: Number(ctx.heartRate) || 0,
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
        { patientId: ctx.patientId, encounterId: ctx.encounterId, userId: Number(ctx.userId) || ctx.userId, priority: ctx.priority, group: ctx.group, dueDate: ctx.dueDate, notes: ctx.notes, categoryId: ctx.categoryId },
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
      { key: "content", label: "Content", default: "" },
    ],
    bodyTemplate: (ctx) =>
      `<ReferralDocument><ID></ID><DocumentID>${ctx.docId}</DocumentID><PatiendID>${ctx.pmsPatientId}</PatiendID><EncounterID>${ctx.pmsEncounterId}</EncounterID><ProviderID>${ctx.providerId}</ProviderID><Type>${ctx.docType}</Type><ItemType>${ctx.itemType}</ItemType><CreatedDate>${ctx.createdDate}</CreatedDate><ContentType>${ctx.contentType}</ContentType><Content>${ctx.content}</Content></ReferralDocument>`,
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
          AmountInclGST: Number(ctx.amountInclGst) || 0,
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
