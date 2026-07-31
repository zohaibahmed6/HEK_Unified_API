import { useEffect, useState } from "react";
import { getRecentCallLogs, type CallLogEntry } from "./api";

/**
 * FR-12/FR-6: a plain-English view of "who called, for which patient, what happened, from which
 * server/db" - the same sentences already written to the log file (and shown as the "Call Flow"
 * card after each call), but browsable here without a terminal/Docker CLI. Manual refresh only,
 * matching the rest of the dashboard's manual-run pattern (no auto-polling).
 */
export function LogsPanel() {
  const [entries, setEntries] = useState<CallLogEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);

  const refresh = async () => {
    setLoading(true);
    setError(null);
    try {
      setEntries(await getRecentCallLogs());
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Failed to load logs");
    } finally {
      setLoading(false);
      setLoaded(true);
    }
  };

  useEffect(() => {
    refresh();
  }, []);

  return (
    <div className="dash">
      <section className="dash-summary">
        <div className="dash-summary-counts">
          <span className="count count--idle">{entries.length} entries</span>
        </div>
        <button type="button" className="dash-run-all-btn" onClick={refresh} disabled={loading}>
          {loading ? "Loading…" : "Refresh"}
        </button>
      </section>

      {error && <div className="dash-auth-status dash-auth-status--error">{error}</div>}

      {loaded && !error && entries.length === 0 && (
        <div className="dash-hint">No call-log entries yet - make a call (e.g. HSS Authenticate) and refresh.</div>
      )}

      <ul className="logs-list">
        {entries.map((e, i) => (
          <li key={i} className="logs-list-item">
            <span className="logs-list-timestamp">
              <span className="logs-list-system">{e.system.toUpperCase()}</span> {e.timestamp}
              {e.correlationId && (
                <span className="logs-list-correlation" title="Matches this same request's entry in technical-*.log / errors-*.log">
                  id: {e.correlationId}
                </span>
              )}
            </span>
            <span className="logs-list-text">{e.text}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
