import { useState } from "react";
import { Activity, FileText, ClipboardList, Receipt, Menu, X, Stethoscope, ScrollText } from "lucide-react";
import { systems, type SystemId } from "./systems";
import { SystemDashboard } from "./SystemDashboard";
import { LogsPanel } from "./LogsPanel";
import { useDashboardStore } from "./store";
import "./App.css";

const SYSTEM_ICONS: Record<SystemId, React.ComponentType<{ size?: number; className?: string }>> = {
  hiso: Activity,
  karo: Stethoscope,
  erms: FileText,
  col: Receipt,
};

type NavId = SystemId | "logs";

function App() {
  const [active, setActive] = useState<NavId>("hiso");
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const isLogs = active === "logs";
  const current = systems.find((s) => s.id === active);
  const { state, setValue, setAuth } = useDashboardStore();

  const selectSystem = (id: NavId) => {
    setActive(id);
    setMobileNavOpen(false);
  };

  return (
    <div className="shell" data-theme={isLogs ? "hiso" : active}>
      {/* Mobile top bar: hamburger + brand, hidden on desktop */}
      <div className="flex items-center justify-between gap-3 border-b border-[var(--line)] bg-white px-4 py-3 md:hidden">
        <button
          type="button"
          aria-label={mobileNavOpen ? "Close navigation" : "Open navigation"}
          onClick={() => setMobileNavOpen((v) => !v)}
          className="rounded-md p-2 text-[var(--ink)] hover:bg-[var(--bg)]"
        >
          {mobileNavOpen ? <X size={22} /> : <Menu size={22} />}
        </button>
        <div className="flex items-center gap-2">
          <ClipboardList size={18} className="text-[var(--accent)]" />
          <span className="font-semibold text-[var(--accent)]">HEK</span>
        </div>
        <span className="w-9" />
      </div>

      {/* Mobile nav overlay */}
      {mobileNavOpen && (
        <div className="fixed inset-0 z-40 flex md:hidden">
          <div className="w-72 max-w-[80vw] overflow-y-auto bg-white shadow-xl">
            <aside className="sidebar !border-r-0">
              <div className="brand">
                <span className="brand-mark">HEK</span>
                <span className="brand-sub">Legacy System Client</span>
              </div>
              <nav>
                {systems.map((s) => {
                  const Icon = SYSTEM_ICONS[s.id];
                  return (
                    <button
                      key={s.id}
                      className={`nav-item nav-item--${s.id} ${s.id === active ? "is-active" : ""}`}
                      onClick={() => selectSystem(s.id)}
                    >
                      <Icon size={16} />
                      {s.label}
                    </button>
                  );
                })}
                <button
                  className={`nav-item nav-item--logs ${isLogs ? "is-active" : ""}`}
                  onClick={() => selectSystem("logs")}
                >
                  <ScrollText size={16} />
                  Logs
                </button>
              </nav>
            </aside>
          </div>
          <div className="flex-1 bg-black/30" onClick={() => setMobileNavOpen(false)} />
        </div>
      )}

      {/* Desktop persistent sidebar */}
      <aside className="sidebar hidden md:flex">
        <div className="brand">
          <span className="brand-mark">HEK</span>
          <span className="brand-sub">Legacy System Client</span>
        </div>
        <nav>
          {systems.map((s) => {
            const Icon = SYSTEM_ICONS[s.id];
            return (
              <button
                key={s.id}
                className={`nav-item nav-item--${s.id} ${s.id === active ? "is-active" : ""}`}
                onClick={() => selectSystem(s.id)}
              >
                <Icon size={16} />
                {s.label}
              </button>
            );
          })}
          <button
            className={`nav-item nav-item--logs ${isLogs ? "is-active" : ""}`}
            onClick={() => selectSystem("logs")}
          >
            <ScrollText size={16} />
            Logs
          </button>
        </nav>
      </aside>

      <main className="content">
        {isLogs ? (
          <>
            <header className="content-header">
              <h1>Logs</h1>
              <p className="content-subtitle">Plain-English call history</p>
            </header>
            <section className="panel">
              <p className="panel-description">
                Every entry below is a real call through the hub, in plain English: who called, for which
                patient, what happened, and which database server/environment served it.
              </p>
            </section>
            <LogsPanel />
          </>
        ) : (
          <>
            <header className="content-header">
              <h1>{current!.label}</h1>
              <p className="content-subtitle">{current!.fullName}</p>
            </header>

            <section className="panel">
              <div className="panel-row">
                <span className="panel-label">Auth</span>
                <span className="panel-value">{current!.authKind}</span>
              </div>
              <p className="panel-description">{current!.description}</p>
            </section>

            <SystemDashboard
              system={current!.id}
              auth={state[current!.id]}
              onSetValue={(key, value) => setValue(current!.id, key, value)}
              onSetAuth={(patch) => setAuth(current!.id, patch)}
            />
          </>
        )}

        <section className="panel panel--callflow">
          <div className="panel-row">
            <span className="panel-label">Call flow</span>
            <span className="panel-value">Real request tracing</span>
          </div>
          <p className="panel-description">
            Every call above is a real request through this hub - traced end to end (origin → routing →
            fulfillment) via OpenTelemetry, already wired for all four systems including this SOAP
            facade. View the live trace for the request you just made:
          </p>
          <a className="callflow-link" href="http://localhost:18888/traces" target="_blank" rel="noreferrer">
            Open live traces →
          </a>
        </section>
      </main>
    </div>
  );
}

export default App;
