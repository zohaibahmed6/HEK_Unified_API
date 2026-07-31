import { forwardRef, useImperativeHandle, useState } from "react";
import { endpoints as allEndpoints, type EndpointDef } from "./catalog";
import { API_BASE } from "./api";
import { downloadBase64File } from "./downloadFile";
import { prettyLabel, KeyValue } from "./viewHelpers";
import { runEndpoint, type RunStatus } from "./runner";

/** One human-readable field pulled out of HISO's submittedDataXml (or a request template). */
interface HisoField {
  name: string;
  value: string;
}

/** A named group of fields (mirrors <section>/<group> nesting in the real XML), can nest further sections. */
interface HisoSection {
  name: string;
  conceptName?: string;
  referenceID?: string;
  fields: HisoField[];
  sections: HisoSection[];
}

/** Walks HISO's real `<dataContainer><section name="..."><field name="..">value</field></section></dataContainer>` shape. */
function parseSubmittedDataXml(xml: string): HisoSection {
  const doc = new DOMParser().parseFromString(xml, "text/xml");
  const root: HisoSection = { name: "Data", fields: [], sections: [] };
  if (doc.querySelector("parsererror")) {
    return root;
  }

  function walk(el: Element, into: HisoSection) {
    for (const child of Array.from(el.children)) {
      const tag = child.tagName.toLowerCase();
      const name = child.getAttribute("name") ?? child.getAttribute("conceptName") ?? tag;
      if (tag === "field") {
        into.fields.push({ name, value: child.textContent?.trim() ?? "" });
      } else if (tag === "section" || tag === "group") {
        const sub: HisoSection = {
          name,
          conceptName: child.getAttribute("conceptName") ?? undefined,
          referenceID: child.getAttribute("referenceID") || undefined,
          fields: [],
          sections: [],
        };
        walk(child, sub);
        into.sections.push(sub);
      } else {
        // Unknown wrapper element (e.g. the outer dataContainer) - keep descending without adding a level.
        walk(child, into);
      }
    }
  }

  const top = doc.documentElement;
  if (top) walk(top, root);
  return root;
}

function SectionView({ section }: { section: HisoSection }) {
  if (section.fields.length === 0 && section.sections.length === 0) {
    return <div className="hiso-empty">No fields returned.</div>;
  }
  return (
    <div className="hiso-section">
      {section.fields.length > 0 && (
        <dl className="hiso-field-list">
          {section.fields.map((f, i) => (
            <div className="hiso-field-row" key={f.name + i}>
              <dt>{prettyLabel(f.name)}</dt>
              <dd>{f.value === "" ? <span className="hiso-empty-val">—</span> : f.value}</dd>
            </div>
          ))}
        </dl>
      )}
      {section.sections.map((s, i) => (
        <div className="hiso-subsection" key={s.name + i}>
          <div className="hiso-subsection-title">{prettyLabel(s.name)}</div>
          <SectionView section={s} />
        </div>
      ))}
    </div>
  );
}

// Real legacy section names, per section, grouped into tabs - grouping mirrors
// legacy-reference/referencehisodata.txt's own section layout exactly, no invented structure.
const TAB_DEFS: { label: string; sections: string[] }[] = [
  {
    label: "Demographics",
    sections: [
      "patient.details",
      "patient.contact.details",
      "patient.residential.address",
      "patient.postal.address",
      "patient.ethnicities",
      "patient.disabilities",
      "patient.language",
      "administrative.eligibility.details",
    ],
  },
  { label: "Documents", sections: ["scannedDocuments"] },
  { label: "Diagnostic Reports", sections: ["clinical.diagnosticReports"] },
  { label: "Clinical History", sections: ["clinical.medicalHistory"] },
  { label: "Medications & Warnings", sections: ["clinical.medications"] },
  { label: "Vitals", sections: ["measurements"] },
  { label: "ACC Claim", sections: ["clinical.general"] },
  { label: "Referrer / Recipient", sections: ["administrative", "recipient", "referrer.details"] },
];

const DOWNLOADABLE_GROUP_CONCEPTS = new Set(["Patient_Attachment", "Patient_IncomingLetter", "Patient_OutgoingLetter"]);

// Legacy's Patient_*_DataType field comes back as either a short label ("PDF") or a real MIME type
// ("application/pdf") depending on the document - normalize both to a real MIME type + extension so
// the browser saves the file as what it actually is instead of a generic/text download.
function resolveDocumentFormat(rawType: string): { mime: string; ext: string } {
  const t = (rawType || "").trim().toLowerCase();
  const known: Record<string, { mime: string; ext: string }> = {
    pdf: { mime: "application/pdf", ext: ".pdf" },
    "application/pdf": { mime: "application/pdf", ext: ".pdf" },
    jpg: { mime: "image/jpeg", ext: ".jpg" },
    jpeg: { mime: "image/jpeg", ext: ".jpg" },
    "image/jpeg": { mime: "image/jpeg", ext: ".jpg" },
    png: { mime: "image/png", ext: ".png" },
    "image/png": { mime: "image/png", ext: ".png" },
    tif: { mime: "image/tiff", ext: ".tif" },
    tiff: { mime: "image/tiff", ext: ".tif" },
    "image/tiff": { mime: "image/tiff", ext: ".tif" },
    doc: { mime: "application/msword", ext: ".doc" },
    docx: { mime: "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ext: ".docx" },
    html: { mime: "text/html", ext: ".html" },
    "text/html": { mime: "text/html", ext: ".html" },
    txt: { mime: "text/plain", ext: ".txt" },
    "text/plain": { mime: "text/plain", ext: ".txt" },
  };
  if (known[t]) return known[t];
  if (t.includes("/")) {
    const ext = "." + t.split("/")[1].replace(/[^a-z0-9]/g, "");
    return { mime: rawType, ext: ext.length > 1 ? ext : ".bin" };
  }
  return { mime: "application/pdf", ext: ".pdf" };
}

function DocumentRow({ group, sessionKey }: { group: HisoSection; sessionKey?: string }) {
  const [downloading, setDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const field = (n: string) => group.fields.find((f) => f.name === n)?.value ?? "";
  const name = field("name") || field("subject") || "Untitled document";
  const date = field("date");
  const dataType = field("type");
  const conceptName = group.conceptName ?? "";
  const canDownload = Boolean(group.referenceID) && DOWNLOADABLE_GROUP_CONCEPTS.has(conceptName) && Boolean(sessionKey);

  const doDownload = async () => {
    setDownloading(true);
    setError(null);
    try {
      const xml =
        `<dataContainer><section name="scannedDocuments">` +
        `<group name="scannedDocument" conceptName="${conceptName}" referenceID="${group.referenceID}">` +
        `<field name="name" conceptName="${conceptName}_Name" />` +
        `<field name="dataType" conceptName="${conceptName}_DataType" />` +
        `<field name="content" conceptName="${conceptName}_Content" />` +
        `</group></section></dataContainer>`;
      const res = await fetch(`${API_BASE}/hiso/getData`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sessionKey,
          dataContainer: { formMetaData: { formInstanceOperationMode: "N" }, submittedDataXml: xml },
        }),
      });
      const text = await res.json();
      const container = text?.getDataResponseReturn?.dataContainer ?? text?.GetDataResponseReturn?.DataContainer;
      const contentXml = container?.submittedDataXml ?? container?.SubmittedDataXml;
      if (!contentXml) {
        setError("No content returned for this document.");
        return;
      }
      const parsed = parseSubmittedDataXml(contentXml);
      const flat: HisoField[] = [];
      const collect = (s: HisoSection) => {
        flat.push(...s.fields);
        s.sections.forEach(collect);
      };
      collect(parsed);
      const content = flat.find((f) => f.name === "content")?.value ?? "";
      const rawType = flat.find((f) => f.name === "dataType")?.value || dataType || "";
      const rawName = flat.find((f) => f.name === "name")?.value || name;
      if (!content) {
        setError("Document content is empty.");
        return;
      }
      const { mime, ext } = resolveDocumentFormat(rawType);
      const fname = /\.[a-z0-9]{2,4}$/i.test(rawName) ? rawName : `${rawName}${ext}`;
      downloadBase64File(content, fname, mime);
    } catch {
      setError("Download failed.");
    } finally {
      setDownloading(false);
    }
  };

  return (
    <div className="hiso-doc-row">
      <div className="hiso-doc-info">
        <div className="hiso-doc-name">{name}</div>
        <div className="hiso-doc-meta">
          {date || "—"} {dataType ? `· ${dataType}` : ""}
        </div>
      </div>
      {canDownload ? (
        <button type="button" className="hiso-doc-download" onClick={doDownload} disabled={downloading}>
          {downloading ? "Downloading…" : "Download"}
        </button>
      ) : (
        <span className="hiso-doc-download-na">—</span>
      )}
      {error && <div className="hiso-doc-error">{error}</div>}
    </div>
  );
}

function TabPanel({ sections, sessionKey }: { sections: HisoSection[]; sessionKey?: string }) {
  const isDocumentsPanel = sections.some((s) => s.name === "scannedDocuments" || s.name === "clinical.diagnosticReports");
  if (sections.every((s) => s.fields.length === 0 && s.sections.length === 0)) {
    return <div className="hiso-empty">No data returned for this section.</div>;
  }
  if (isDocumentsPanel) {
    const groups = sections.flatMap((s) => s.sections);
    if (groups.length === 0) return <div className="hiso-empty">No documents found.</div>;
    return (
      <div className="hiso-doc-list">
        {groups.map((g, i) => (
          <DocumentRow key={(g.referenceID ?? "") + i} group={g} sessionKey={sessionKey} />
        ))}
      </div>
    );
  }
  return (
    <div>
      {sections.map((s, i) => (
        <div key={s.name + i} className="hiso-tab-section">
          {sections.length > 1 && <div className="hiso-tab-section-title">{prettyLabel(s.name)}</div>}
          <SectionView section={s} />
        </div>
      ))}
    </div>
  );
}

// Mirrors the legacy HealthLink form's left-nav style (bold section name + one-line summary
// underneath, e.g. "1 long term medication specified" in legacy-reference/legacy_ui_images/hiso_form_data.png).
function summarizeSections(sections: HisoSection[]): string {
  let filled = 0;
  let total = 0;
  let groups = 0;
  const walk = (s: HisoSection, isTop: boolean) => {
    total += s.fields.length;
    filled += s.fields.filter((f) => f.value !== "").length;
    if (isTop) groups += s.sections.length;
    s.sections.forEach((sub) => walk(sub, false));
  };
  sections.forEach((s) => walk(s, true));
  if (filled === 0 && groups === 0) return "No data";
  const parts: string[] = [];
  if (groups > 0) parts.push(`${groups} record${groups === 1 ? "" : "s"}`);
  if (total > 0) parts.push(`${filled}/${total} fields`);
  return parts.join(" · ");
}

/** Full patient record: left-side tabs, each mapped 1:1 to a real legacy `<section name>` group. */
function HisoPatientRecordView({ root, sessionKey }: { root: HisoSection; sessionKey?: string }) {
  const [activeTab, setActiveTab] = useState(0);
  const byName = new Map(root.sections.map((s) => [s.name, s]));
  const tabs = TAB_DEFS.map((t) => ({
    label: t.label,
    sections: t.sections.map((n) => byName.get(n)).filter((s): s is HisoSection => Boolean(s)),
  })).filter((t) => t.sections.length > 0);

  if (tabs.length === 0) {
    return <div className="hiso-empty">Session was valid, but no form data was returned.</div>;
  }

  return (
    <div className="hiso-record">
      <div className="hiso-record-tabs">
        {tabs.map((t, i) => (
          <button
            key={t.label}
            type="button"
            className={`hiso-tab-btn ${i === activeTab ? "is-active" : ""}`}
            onClick={() => setActiveTab(i)}
          >
            <span className="hiso-tab-btn-label">{t.label}</span>
            <span className="hiso-tab-btn-summary">{summarizeSections(t.sections)}</span>
          </button>
        ))}
      </div>
      <div className="hiso-record-panel">
        <TabPanel sections={tabs[activeTab].sections} sessionKey={sessionKey} />
      </div>
    </div>
  );
}

export interface HisoPatientFormHandle {
  run: () => Promise<void>;
}

/**
 * The single HISO "form": one Load button, no endpoint picker - mirrors the legacy HealthLink
 * form UX (legacy-reference/legacy_ui_images) where the user never sees individual API calls,
 * just a sectioned patient record. Internally this is the one `getData` call that already
 * returns every section in one shot (see FULL_RECORD_REQUEST_XML in catalog.ts).
 */
export const HisoPatientForm = forwardRef<
  HisoPatientFormHandle,
  { contextValues: Record<string, string>; sessionKey?: string; onStatus?: (status: RunStatus) => void }
>(function HisoPatientForm({ contextValues, sessionKey, onStatus }, ref) {
  const endpoint = allEndpoints.find((e) => e.id === "hiso-getData-record")!;
  const [status, setStatus] = useState<RunStatus>("idle");
  const [root, setRoot] = useState<HisoSection | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [durationMs, setDurationMs] = useState<number | undefined>(undefined);
  // FR-12 call-flow traceability: plain-English routing sentence from X-Hek-Routing-Summary.
  const [routingSummary, setRoutingSummary] = useState<string | null>(null);

  const run = async () => {
    setStatus("loading");
    onStatus?.("loading");
    setError(null);
    const merged = { ...contextValues, sessionKey: sessionKey ?? "" };
    const body = endpoint.bodyTemplate?.(merged) ?? "";
    const r = await runEndpoint(endpoint, merged, { sessionKey }, body);
    setDurationMs(r.durationMs);
    setStatus(r.status);
    onStatus?.(r.status);
    setRoutingSummary(r.routingSummary ?? null);

    if (r.status === "error") {
      setError(r.summary);
      setRoot(null);
      return;
    }
    try {
      const parsed = JSON.parse(r.raw);
      const rr = parsed?.getDataResponseReturn ?? parsed?.GetDataResponseReturn;
      const container = rr?.dataContainer ?? rr?.DataContainer;
      const xml = container?.submittedDataXml ?? container?.SubmittedDataXml;
      setRoot(xml ? parseSubmittedDataXml(xml) : null);
    } catch {
      setRoot(null);
    }
  };

  useImperativeHandle(ref, () => ({ run }));

  return (
    <div className="ep-card hiso-patient-form">
      <div className="hiso-form-head">
        <div className="hiso-form-title">Patient Record</div>
        <button type="button" className="ep-run-btn" onClick={run} disabled={status === "loading" || !sessionKey}>
          {status === "loading" ? "Loading…" : "Load Patient Record"}
        </button>
        {durationMs !== undefined && <span className="ep-duration">{durationMs} ms</span>}
      </div>

      {routingSummary && (
        <div className="ep-call-flow">
          <span className="ep-call-flow-label">Call Flow</span> {routingSummary}
        </div>
      )}
      {error && <div className="hiso-doc-error">{error}</div>}
      {status === "empty" && !root && (
        <div className="hiso-empty">Session was valid, but no form data was returned (static/parked mode).</div>
      )}
      {root && <HisoPatientRecordView root={root} sessionKey={sessionKey} />}
      {!root && status === "idle" && <div className="hiso-empty">Click "Load Patient Record" to fetch the full record.</div>}
    </div>
  );
});

/**
 * Turns a raw HISO JSON response into a labeled, sectioned view instead of a JSON dump.
 * Returns null when the endpoint isn't HISO or the payload doesn't parse - callers should fall
 * back to the existing raw-response display in that case.
 */
export function renderHisoResult(endpoint: EndpointDef, raw: string, sessionKey?: string): React.ReactNode | null {
  if (endpoint.system !== "hiso") return null;

  let parsed: any;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return null;
  }

  switch (endpoint.id) {
    case "hiso-getVersion": {
      const r = parsed?.getVersionResponseReturn ?? parsed?.GetVersionResponseReturn;
      if (!r) return null;
      return (
        <KeyValue
          pairs={[
            ["Application", r.application ?? r.Application],
            ["Application Version", r.applicationVersion ?? r.ApplicationVersion],
            ["HISO Version", String(r.hisoversion ?? r.Hisoversion ?? "")],
          ]}
        />
      );
    }

    case "hiso-getDeliveryOptions": {
      const r = parsed?.getDeliveryOptionsResponseReturn ?? parsed?.GetDeliveryOptionsResponseReturn;
      if (!r) return null;
      return (
        <KeyValue
          pairs={[
            ["Delivery URL", r.url ?? r.URL],
            ["Message ID", r.messageID ?? r.MessageID],
            ["Recipient Account", r.recipientAccount ?? r.RecipientAccount],
            ["Sender Account", r.senderAccount ?? r.SenderAccount],
            ["Sender Password", r.senderPassword ? "•••••• (set)" : ""],
          ]}
        />
      );
    }

    case "hiso-processAction": {
      const r = parsed?.processActionResponseReturn ?? parsed?.ProcessActionResponseReturn;
      if (!r) return null;
      const processed = r.processed ?? r.Processed;
      return <div className={`hiso-outcome ${processed ? "hiso-outcome--ok" : "hiso-outcome--no"}`}>{processed ? "Action processed" : "Action not processed"}</div>;
    }

    case "hiso-saveContainer": {
      const r = parsed?.saveContainerResponseReturn ?? parsed?.SaveContainerResponseReturn;
      if (!r) return null;
      const saved = r.response ?? r.Response;
      return <div className={`hiso-outcome ${saved ? "hiso-outcome--ok" : "hiso-outcome--no"}`}>{saved ? "Saved successfully" : "Save failed"}</div>;
    }

    case "hiso-getData-record": {
      const r = parsed?.getDataResponseReturn ?? parsed?.GetDataResponseReturn;
      const container = r?.dataContainer ?? r?.DataContainer;
      if (!container) {
        return <div className="hiso-empty">Session was valid, but no form data was returned (static/parked mode - a real empty stub, not an error).</div>;
      }
      const xml = container.submittedDataXml ?? container.SubmittedDataXml;
      if (!xml) return <div className="hiso-empty">No submitted data.</div>;
      const root = parseSubmittedDataXml(xml);
      return <HisoPatientRecordView root={root} sessionKey={sessionKey} />;
    }

    case "hiso-getFormView": {
      const r = parsed?.getFormViewResponseReturn ?? parsed?.GetFormViewResponseReturn;
      if (!r) return null;
      const view = r.view ?? r.View;
      return (
        <div>
          <KeyValue
            pairs={[
              ["Resume Path", r.resumePath ?? r.ResumePath],
              ["View Type", r.viewType ?? r.ViewType],
            ]}
          />
          {view && (
            <details className="hiso-view-html">
              <summary>Form view content ({(r.viewType ?? r.ViewType ?? "content").toString()})</summary>
              <pre>{view}</pre>
            </details>
          )}
        </div>
      );
    }

    default:
      return null;
  }
}
