# HEK Unified Healthcare API Hub — Specification

**Project:** HEK Unified Healthcare API (API Hub / Integration Gateway)
**Source:** Requirements stated by Dr. Ahmed Javad in the "AI Transition Platform — 1st Task demo" meeting (recorded, transcript on file)
**Author:** Zohaib Ahmed
**Date:** 2026-07-22
**Status:** Draft v1.0

---

## 1. Purpose and Vision

Today, three separate integrations exist against HEK systems: **HISO**, **Karo**, and **ERMS**. Each external system calls its own API and requests its own data fields (e.g., HISO uses ~100 fields, Karo ~50, ERMS ~100; combined roughly 300 fields with overlap).

Dr. Javad's directive: **replace these with one single, unified REST API — an API Hub / Gateway** — that any consuming system talks to. Existing per-system APIs will be deprecated once the hub is live ("we'll tell the Karo team: the old API is being shut down, use this now"). Long-term, *every* HEK integration — current and future — routes through this hub, and the hub becomes the single managed integration point.

This is explicitly described as **one of the major projects HEK will undertake going forward**, and the first deliverable is a working, presentable R&D demo.

## 2. Scope

### In scope (Phase 1)
1. Analyze the three existing integrations (HISO, Karo, ERMS) and extract each system's **data set** separately.
2. Derive the **common data elements** across the three to form one unified/canonical data set.
3. Design and build a **single unified REST API** exposing that data set.
4. Field-level authorization, audit logging, telemetry, error handling, containerized deployment.
5. Simulation/demo: simulate each consumer (e.g., "the HISO caller") hitting the hub and receiving exactly what its old API gave it.

### Out of scope (Phase 1)
- Migrating consumers in production (deprecation of old APIs is a later phase).
- Integrations beyond the initial three (the design must *support* them, not implement them).

## 3. Functional Requirements

| ID | Requirement |
|----|-------------|
| FR-1 | Extract and document the data set of each of the three integrations (HISO, Karo, ERMS) individually. |
| FR-2 | Identify common elements across the three data sets and define one unified/canonical data model. |
| FR-3 | Expose a single REST API set; any consuming system calls the same API and states what it needs. |
| FR-4 | The API must return **only the data requested** — no "return everything" endpoints. It must be an intelligent system that serves precisely what a consumer asks for, not a single stored procedure that dumps all data. |
| FR-5 | Per-consumer field scoping: each consumer may access **only the fields defined in its own standard** (HISO consumers get HISO's fields, Karo consumers Karo's, etc.). A consumer must never receive fields outside its scope. "No extra data goes to anyone." |
| FR-6 | Full request auditing: record **which consumer called, when, and exactly which data/fields were taken**, so a complete access history exists. |
| FR-7 | Extensibility: new systems/integrations can be added to the hub over time without redesign — the API is extended with additional integrations. |
| FR-8 | The hub becomes the gateway for all HEK integrations; design must anticipate onboarding the remaining integrations after the first three. |
| FR-9 | Simulation capability for the demo: emulate each existing consumer's calls against the hub to prove parity with the current per-system APIs. |

## 4. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-1 | **Technology:** latest/most modern API technology. Implementation in **.NET Core (ASP.NET Core Web API)**, REST. |
| NFR-2 | **Deployment:** containerized (Docker), hosting-agnostic — deployable anywhere, portable. |
| NFR-3 | **Scalability:** scales up and down properly (container-based horizontal scaling). |
| NFR-4 | **Security:** top-notch. Gateway-level authentication (defined method per consumer), authorization enforcing field-level scopes (FR-5). |
| NFR-5 | **Telemetry:** full telemetry of every kind built into the API. |
| NFR-6 | **Logging & tracking:** all forms of login/call tracking. |
| NFR-7 | **Error handling:** systematic, well-designed error handling throughout. |
| NFR-8 | **Code quality:** minimal, clean code — use available skills/patterns that minimize code ("less code, less mess, less cost"). |
| NFR-9 | **Research-grounded design:** study how major platforms (Azure, Amazon/AWS API gateways) approach this; follow the latest industry patterns. |

## 5. Process Requirements (from Dr. Javad)

1. **Study the three systems first** — their data sets, how their APIs are called, and how consumers authenticate — before building.
2. **AI-assisted development with proper guidance:** development is done with Claude, but the developer must direct it with full architectural understanding (give it the right "animal to build," not vague prompts). Dr. Javad will **review the entire Claude conversation/development model**.
3. **Proper R&D first:** research is expected to take a day or more; a full working model of how the system will be built must exist before/alongside the demo.
4. **Presentation quality:** the deliverable must be smart and presentable — **no spelling mistakes, no sloppiness**.
5. **Deadline:** working demo presented **Wednesday**.
6. **Approval gate:** the approach is an experiment Dr. Javad will approve — demonstrate that nothing better could be built.

## 6. Proposed Architecture (to satisfy the above)

- **API Gateway / Hub** (ASP.NET Core, containerized): single entry point, versioned REST endpoints.
- **Canonical data model** built from the common elements of HISO/Karo/ERMS data sets; per-consumer **field-scope profiles** stored as configuration.
- **AuthN/AuthZ:** OAuth 2.0 client-credentials (JWT) per consumer; scopes map to field profiles; middleware strips any field outside the caller's profile before response serialization.
- **Selective retrieval:** consumers specify required fields (field-selection query / sparse fieldsets); server intersects request with the consumer's allowed scope.
- **Audit service:** every request logged with consumer identity, timestamp, endpoint, and exact fields returned.
- **Observability:** OpenTelemetry (traces, metrics, logs), structured logging, health endpoints.
- **Error contract:** RFC 7807 problem+json across all endpoints.
- **Extensibility:** each source system behind an adapter; onboarding a new integration = new adapter + field-scope profile, no core changes.

## 7. Acceptance Criteria (Wednesday demo)

1. Three documented data sets (HISO, Karo, ERMS) and the derived unified data set.
2. Running containerized .NET Core API with at least the unified endpoints needed to reproduce what each of the three consumers gets today.
3. Simulated calls per consumer showing: correct data returned, out-of-scope fields blocked, audit record produced, telemetry visible.
4. Documented authentication method and gateway usage rules for consumers.
5. Clean, minimal codebase; presentation materials free of spelling errors; full Claude development conversation available for review.

## 8. Open Items (to confirm with Dr. Javad / source teams)

- Exact field inventories of HISO, Karo, and ERMS (requires access to their API docs/source).
- Authentication mechanisms currently used by each consumer.
- Hosting target for the demo (local Docker vs. cloud).

---

*Change Log*
- v1.0 (2026-07-22): Initial spec extracted from meeting transcript.
