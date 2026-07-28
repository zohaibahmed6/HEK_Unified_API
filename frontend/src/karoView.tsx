import type { EndpointDef } from "./catalog";
import { prettyLabel, KeyValue } from "./viewHelpers";

/**
 * Turns a raw KARO/HSS JSON response into a labeled view instead of a raw JSON dump.
 * Shape copied 1:1 from legacy-reference/hsskaro.txt - a consistent
 * `{patientId, resourceType, system, entry:[...]}` envelope; demographics nests one extra
 * `listDemographicInfo` level, everything else is a flat array of records under `entry`.
 * Returns null when the endpoint isn't KARO or the payload doesn't parse - callers fall back
 * to the raw-response display in that case.
 */
export function renderKaroResult(endpoint: EndpointDef, raw: string): React.ReactNode | null {
  if (endpoint.system !== "karo") return null;
  if (endpoint.id === "karo-ping") return null;

  let parsed: any;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return null;
  }
  if (!parsed || typeof parsed !== "object") return null;

  let entries: Record<string, unknown>[] = Array.isArray(parsed.entry) ? parsed.entry : [];

  // Demographics nests the real record one level deeper: entry[0].listDemographicInfo[0].
  if (endpoint.id === "karo-demographics" && entries.length > 0 && Array.isArray((entries[0] as any).listDemographicInfo)) {
    entries = (entries[0] as any).listDemographicInfo as Record<string, unknown>[];
  }

  if (entries.length === 0) {
    // Single-object responses (e.g. provider/document metadata without an entry envelope).
    const own = Object.entries(parsed).filter(([k]) => !["patientId", "resourceType", "system", "entry"].includes(k));
    if (own.length === 0) return <div className="hiso-empty">No data returned.</div>;
    return <KeyValue pairs={own.map(([k, v]) => [prettyLabel(k), v == null ? "" : String(v)] as [string, string])} />;
  }

  return (
    <div className="hiso-doc-list">
      {entries.map((entry, i) => (
        <div className="hiso-subsection" key={i}>
          <KeyValue pairs={Object.entries(entry).map(([k, v]) => [prettyLabel(k), v == null ? "" : String(v)] as [string, string])} />
        </div>
      ))}
    </div>
  );
}
