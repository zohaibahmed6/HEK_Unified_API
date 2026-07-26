import type { EndpointDef } from "./catalog";

/** One human-readable field pulled out of HISO's submittedDataXml (or a request template). */
interface HisoField {
  name: string;
  value: string;
}

/** A named group of fields (mirrors <section>/<group> nesting in the real XML), can nest further sections. */
interface HisoSection {
  name: string;
  fields: HisoField[];
  sections: HisoSection[];
}

function prettyLabel(name: string): string {
  return name
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/^./, (c) => c.toUpperCase());
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
        const sub: HisoSection = { name, fields: [], sections: [] };
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

function KeyValue({ pairs }: { pairs: [string, string | undefined | null][] }) {
  return (
    <dl className="hiso-field-list">
      {pairs.map(([label, value]) => (
        <div className="hiso-field-row" key={label}>
          <dt>{label}</dt>
          <dd>{value ? value : <span className="hiso-empty-val">—</span>}</dd>
        </div>
      ))}
    </dl>
  );
}

/**
 * Turns a raw HISO JSON response into a labeled, sectioned view instead of a JSON dump.
 * Returns null when the endpoint isn't HISO or the payload doesn't parse - callers should fall
 * back to the existing raw-response display in that case.
 */
export function renderHisoResult(endpoint: EndpointDef, raw: string): React.ReactNode | null {
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

    case "hiso-getData-demographics":
    case "hiso-getData-attachment": {
      const r = parsed?.getDataResponseReturn ?? parsed?.GetDataResponseReturn;
      const container = r?.dataContainer ?? r?.DataContainer;
      if (!container) {
        return <div className="hiso-empty">Session was valid, but no form data was returned (static/parked mode - a real empty stub, not an error).</div>;
      }
      const xml = container.submittedDataXml ?? container.SubmittedDataXml;
      if (!xml) return <div className="hiso-empty">No submitted data.</div>;
      const section = parseSubmittedDataXml(xml);
      return <SectionView section={section} />;
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
