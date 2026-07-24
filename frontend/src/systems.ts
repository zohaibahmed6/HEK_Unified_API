export type SystemId = "hiso" | "karo" | "erms" | "col";

export interface SystemDef {
  id: SystemId;
  label: string;
  fullName: string;
  authKind: string;
  description: string;
}

export const systems: SystemDef[] = [
  {
    id: "hiso",
    label: "HISO",
    fullName: "Health Intranet Services Online",
    authKind: "Session GUID (no bearer token)",
    description:
      "SOAP service at /FormSessionService.svc. Reads/writes real ACC45 form data via getData, saveContainer, and processAction.",
  },
  {
    id: "karo",
    label: "KARO",
    fullName: "Indici HSS Web API",
    authKind: "Authenticate → bearer token + environment identifier",
    description:
      "REST API. Authenticate with username/password/system/pho, then call demographics, clinical notes, conditions, and documents with the issued token.",
  },
  {
    id: "erms",
    label: "ERMS",
    fullName: "Indici ERMS Web API",
    authKind: "Authenticate → bearer token + environment identifier",
    description:
      "REST API. Real reads (GetCurrentUser, GetPatientData, GetScannedList) plus a real write endpoint (SaveDocument).",
  },
  {
    id: "col",
    label: "COL",
    fullName: "COL / Pegasus Claiming",
    authKind: "Authenticate → bearer token",
    description:
      "REST API. Real reads (GetCurrentPatientData, GetSessionData, GetProviderData) plus a real financial write endpoint (SaveInvoice).",
  },
];
