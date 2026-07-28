import type { EndpointDef } from "./catalog";
import { prettyLabel, KeyValue } from "./viewHelpers";

interface ErmsRecord {
  referenceID?: string;
  fields: [string, string][];
}

interface ErmsParsed {
  isList: boolean;
  records: ErmsRecord[];
}

/**
 * Walks ERMS's real response shape - a single flat record (e.g. `<PatientData><Patient_Surname>RICE</Patient_Surname>...`)
 * or a `conceptType="List"` root wrapping repeated same-tag records (e.g. `<Accidents conceptType="List"><Patient_Accident>...`).
 * Structure copied 1:1 from legacy-reference/log1_erms_sanitized.txt, not invented.
 */
function parseErmsXml(xml: string): ErmsParsed | null {
  const doc = new DOMParser().parseFromString(xml, "text/xml");
  if (doc.querySelector("parsererror")) return null;
  const root = doc.documentElement;
  if (!root) return null;

  const isList = root.getAttribute("conceptType") === "List";

  const recordFields = (el: Element): [string, string][] =>
    Array.from(el.children).map((f) => [f.tagName, f.textContent?.trim() ?? ""]);

  if (isList) {
    const records = Array.from(root.children).map((child) => ({
      referenceID: child.getAttribute("referenceID") || undefined,
      fields: recordFields(child),
    }));
    return { isList: true, records };
  }

  const fields = recordFields(root);
  if (fields.length === 0) return { isList: false, records: [] };
  return { isList: false, records: [{ fields }] };
}

function ErmsResultView({ parsed }: { parsed: ErmsParsed }) {
  if (parsed.records.length === 0) {
    return <div className="hiso-empty">No data returned.</div>;
  }
  if (!parsed.isList) {
    return <KeyValue pairs={parsed.records[0].fields.map(([n, v]) => [prettyLabel(n), v] as [string, string])} />;
  }
  return (
    <div className="hiso-doc-list">
      {parsed.records.map((r, i) => (
        <div className="hiso-subsection" key={r.referenceID ?? i}>
          <KeyValue pairs={r.fields.map(([n, v]) => [prettyLabel(n), v] as [string, string])} />
        </div>
      ))}
    </div>
  );
}

/**
 * Turns a raw ERMS XML response into a labeled view instead of a raw XML dump.
 * Returns null when the endpoint isn't ERMS or the payload doesn't parse - callers fall back
 * to the raw-response display in that case.
 */
export function renderErmsResult(endpoint: EndpointDef, raw: string): React.ReactNode | null {
  if (endpoint.system !== "erms") return null;
  if (endpoint.id === "erms-ping" || endpoint.id === "erms-SaveDocument") return null;

  const parsed = parseErmsXml(raw);
  if (!parsed) return null;
  return <ErmsResultView parsed={parsed} />;
}
