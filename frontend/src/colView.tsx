import type { EndpointDef } from "./catalog";
import { prettyLabel, KeyValue } from "./viewHelpers";

/**
 * Turns a raw COL JSON response into a labeled view instead of a raw JSON dump.
 * Real shape (ColCompatController.RenderList): a plain JSON array of flat row objects when there
 * are rows, or a single empty object `{}` when there are none - no envelope, unlike KARO/ERMS.
 * Returns null when the endpoint isn't COL or the payload doesn't parse - callers fall back to the
 * raw-response display in that case.
 */
export function renderColResult(endpoint: EndpointDef, raw: string): React.ReactNode | null {
  if (endpoint.system !== "col") return null;
  if (endpoint.id === "col-SaveInvoice") return null;

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return null;
  }

  const rows: Record<string, unknown>[] = Array.isArray(parsed)
    ? (parsed as Record<string, unknown>[])
    : parsed && typeof parsed === "object"
      ? [parsed as Record<string, unknown>]
      : [];

  const hasAnyValue = rows.some((r) => Object.values(r).some((v) => v !== null && v !== undefined && v !== ""));
  if (rows.length === 0 || !hasAnyValue) {
    return <div className="hiso-empty">No data returned.</div>;
  }

  if (rows.length === 1) {
    return <KeyValue pairs={Object.entries(rows[0]).map(([k, v]) => [prettyLabel(k), v == null ? "" : String(v)] as [string, string])} />;
  }

  return (
    <div className="hiso-doc-list">
      {rows.map((row, i) => (
        <div className="hiso-subsection" key={i}>
          <KeyValue pairs={Object.entries(row).map(([k, v]) => [prettyLabel(k), v == null ? "" : String(v)] as [string, string])} />
        </div>
      ))}
    </div>
  );
}
