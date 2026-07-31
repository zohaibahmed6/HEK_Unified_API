import { API_BASE } from "./api";
import type { EndpointDef } from "./catalog";

export type RunStatus = "idle" | "loading" | "success" | "empty" | "error";

export interface RunResult {
  status: RunStatus;
  httpStatus?: number;
  raw: string;
  summary: string;
  durationMs: number;
  /** FR-12 call-flow traceability: plain-English "what happened, where" sentence from the X-Hek-Routing-Summary response header, when the API set one. */
  routingSummary?: string;
}

function buildQuery(values: Record<string, string>): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== undefined && value !== "") {
      params.set(key, value);
    }
  }
  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

/** Looks for the known real "no meaningful data" shapes across all four systems' real response conventions. */
function isEmptyPayload(parsed: unknown): boolean {
  if (parsed === null || parsed === undefined) return true;
  if (Array.isArray(parsed)) return parsed.length === 0;
  if (typeof parsed === "object") {
    const values = Object.values(parsed as Record<string, unknown>);
    if (values.length === 0) return true;
    return values.every((v) => v === null || v === undefined || v === "" || (Array.isArray(v) && v.length === 0));
  }
  return parsed === "";
}

/** Real per-system quirks: ERMS/COL always answer HTTP 200 even on failure, so the error signal lives in the body. */
function detectBodyLevelError(system: EndpointDef["system"], text: string, parsed: unknown): string | null {
  if (system === "erms" && text.includes("<Message>")) {
    const match = text.match(/<Message>([^<]*)<\/Message>/);
    if (match?.[1]) return match[1];
  }
  if (system === "col" && parsed && typeof parsed === "object") {
    const obj = parsed as Record<string, unknown>;
    if (typeof obj.error === "string" && obj.error.length > 0) return obj.error;
  }
  // COL sometimes returns a bare SQL/exception string as the whole body instead of JSON.
  if (system === "col" && typeof parsed !== "object" && text.length > 0 && /error|exception|invalid/i.test(text)) {
    return text.slice(0, 300);
  }
  return null;
}

export async function runEndpoint(
  endpoint: EndpointDef,
  contextValues: Record<string, string>,
  auth: { token?: string; sessionKey?: string },
  bodyOverride?: string,
): Promise<RunResult> {
  const started = performance.now();
  const url = `${API_BASE}${endpoint.path}`;
  const headers: Record<string, string> = {};
  if (endpoint.system !== "hiso" && auth.token) {
    headers["Authorization"] = `Bearer ${auth.token}`;
  }

  let fetchUrl = url;
  let body: string | undefined;

  if (endpoint.method === "GET") {
    fetchUrl = `${url}${buildQuery(contextValues)}`;
  } else {
    body = bodyOverride ?? endpoint.bodyTemplate?.(contextValues) ?? "";
    headers["Content-Type"] = endpoint.bodyKind === "xml" ? "text/xml" : "application/json";
  }

  try {
    const res = await fetch(fetchUrl, { method: endpoint.method, headers, body });
    const text = await res.text();
    const durationMs = Math.round(performance.now() - started);
    const routingSummary = res.headers.get("X-Hek-Routing-Summary") ?? undefined;

    let parsed: unknown = text;
    const contentType = res.headers.get("content-type") ?? "";
    if (contentType.includes("json")) {
      try {
        parsed = JSON.parse(text);
      } catch {
        parsed = text;
      }
    }

    if (!res.ok) {
      return { status: "error", httpStatus: res.status, raw: text, summary: `HTTP ${res.status}`, durationMs, routingSummary };
    }

    const bodyError = detectBodyLevelError(endpoint.system, text, parsed);
    if (bodyError) {
      return { status: "error", httpStatus: res.status, raw: text, summary: bodyError, durationMs, routingSummary };
    }

    if (isEmptyPayload(parsed)) {
      return { status: "empty", httpStatus: res.status, raw: text, summary: "No data returned", durationMs, routingSummary };
    }

    return { status: "success", httpStatus: res.status, raw: text, summary: "OK", durationMs, routingSummary };
  } catch (ex) {
    const durationMs = Math.round(performance.now() - started);
    return { status: "error", raw: String(ex), summary: ex instanceof Error ? ex.message : "Network error", durationMs };
  }
}
