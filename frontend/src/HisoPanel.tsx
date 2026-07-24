import { useState } from "react";
import { hisoGetData, HISO_DEMOGRAPHICS_XML, type HisoGetDataResult } from "./api";

export function HisoPanel() {
  const [sessionKey, setSessionKey] = useState("F4FA4398-3906-4A91-AE8E-70AA044E1672");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<HisoGetDataResult | null>(null);

  async function handleFetch() {
    setLoading(true);
    setResult(null);
    try {
      const res = await hisoGetData(sessionKey, HISO_DEMOGRAPHICS_XML);
      setResult(res);
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="panel">
      <div className="field-row">
        <label htmlFor="hiso-session-key">Session GUID</label>
        <input
          id="hiso-session-key"
          value={sessionKey}
          onChange={(e) => setSessionKey(e.target.value)}
          placeholder="e.g. F4FA4398-3906-4A91-AE8E-70AA044E1672"
        />
      </div>

      <button className="action-btn" onClick={handleFetch} disabled={loading || !sessionKey}>
        {loading ? "Fetching…" : "Fetch demographics (getData)"}
      </button>

      {result && (
        <div className={`result ${result.ok ? "result--ok" : "result--error"}`}>
          {result.ok ? (
            <pre>{result.filledXml ?? "(no data returned - session may not have matched a dynamic-mode form)"}</pre>
          ) : (
            <p>{result.error}</p>
          )}
        </div>
      )}
    </section>
  );
}
