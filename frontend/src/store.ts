import { useCallback, useState } from "react";
import type { SystemId } from "./systems";
import { sharedParams } from "./catalog";

export interface SystemAuthState {
  token?: string;
  sessionKey?: string;
  practiceId?: string;
  username?: string;
  password?: string;
  values: Record<string, string>;
}

const STORAGE_KEY = "hek-dashboard-state-v1";

function defaultValuesFor(system: SystemId): Record<string, string> {
  const values: Record<string, string> = {};
  for (const p of sharedParams[system]) {
    values[p.key] = p.default;
  }
  return values;
}

function loadInitial(): Record<SystemId, SystemAuthState> {
  const systems: SystemId[] = ["hiso", "karo", "erms", "col"];
  const fallback = Object.fromEntries(systems.map((s) => [s, { values: defaultValuesFor(s) }])) as Record<SystemId, SystemAuthState>;

  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return fallback;
    const parsed = JSON.parse(raw) as Partial<Record<SystemId, SystemAuthState>>;
    for (const s of systems) {
      fallback[s] = { ...fallback[s], ...parsed[s], values: { ...fallback[s].values, ...parsed[s]?.values } };
    }
    return fallback;
  } catch {
    return fallback;
  }
}

/** Global per-system auth/context state - token + shared field values persist across tab switches and page reloads. */
export function useDashboardStore() {
  const [state, setState] = useState<Record<SystemId, SystemAuthState>>(loadInitial);

  const persist = useCallback((next: Record<SystemId, SystemAuthState>) => {
    setState(next);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    } catch {
      // ignore - dashboard state is a convenience, not critical data
    }
  }, []);

  const setValue = useCallback(
    (system: SystemId, key: string, value: string) => {
      setState((prev) => {
        const next = { ...prev, [system]: { ...prev[system], values: { ...prev[system].values, [key]: value } } };
        try {
          localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
        } catch {
          // ignore
        }
        return next;
      });
    },
    [],
  );

  const setAuth = useCallback(
    (system: SystemId, patch: Partial<Pick<SystemAuthState, "token" | "sessionKey" | "practiceId" | "username" | "password">>) => {
      setState((prev) => {
        const next = { ...prev, [system]: { ...prev[system], ...patch } };
        try {
          localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
        } catch {
          // ignore
        }
        return next;
      });
    },
    [],
  );

  return { state, setValue, setAuth, persist };
}
