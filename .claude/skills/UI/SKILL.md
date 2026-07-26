---
name: api-dashboard-builder
description: >
  Builds/extends a React + TypeScript + Vite dashboard UI for the HEK
  Unified API that consumes each documented endpoint and renders the data
  in human-readable, sectioned form (like a normal product dashboard) —
  never raw JSON/XML/SOAP dumped on screen. Endpoint list and shapes come
  from the project's own documentation, not auto-discovery. Use when the
  user asks to "build a UI for the API", "make a dashboard", "show this
  data nicely", "convert API responses to a readable interface", or wants
  a new endpoint added to the existing frontend as a proper dashboard
  section.
---

# API Dashboard Builder

## Goal

For each endpoint documented in the project's docs, produce a dashboard
section that: calls the endpoint, takes the response (JSON, XML, or SOAP),
and renders it as labeled fields/cards/tables grouped logically — the way a
real product dashboard shows account info, orders, documents, etc. The raw
payload should never be the thing shown to the user.

## Where to look first

1. Project documentation (confirm exact path with the user if not already
   known — e.g. `HEK_UNIFIED_API_SPEC_v1.1.md`, `HISO_doc.md`, ERMS/KARO/COL
   spec docs, or whatever the repo's docs folder contains) — this is the
   source of truth for endpoint list, request shape, and response fields.
   `grep -n` for endpoint paths/operation names rather than reading full
   docs.
2. The existing frontend (`frontend/` — React+TS+Vite, per the project's
   established architecture: 4 themed sections for HISO/KARO/ERMS/COL, one
   panel per system) — check what's already built before adding new code,
   so sections aren't duplicated.

## Hard rule: minimize tokens

- Don't read whole doc or source files. Grep for the specific endpoint's
  section/operation name and pull just its field list.
- Build one endpoint's section at a time; don't regenerate the whole
  frontend from scratch if only one endpoint changed.
- Reuse existing shared components (form inputs, panel layout, theme
  colors) already in the frontend rather than re-deriving a design system
  each time.

## UI design rules (human-readable, not raw data)

- Never render a raw JSON/XML blob as the primary display. `<pre>{JSON...}`
  dumps are not acceptable as the main view (a collapsed "raw response"
  debug toggle is fine as a secondary, optional element).
- Map each response field to a labeled UI element: a field like
  `dateOfBirth` becomes a labeled "Date of Birth" value, formatted as a
  readable date, not an ISO string; nested objects become their own
  card/subsection with a heading; arrays become tables or lists, not
  bracket-dumped text.
- Group related fields into logical sections (e.g. "Patient Details",
  "Appointment Info", "Documents") based on what the endpoint's data
  actually represents — infer grouping from field names/docs, don't dump
  everything flat.
- Handle empty/error states explicitly (e.g. "No documents found" instead
  of an empty table with no message; a real error message instead of a
  stack trace).
- Follow the existing per-system theme/color already established in the
  frontend for consistency.
- SOAP/XML responses (e.g. HISO) must be parsed and mapped the same way as
  JSON ones before rendering — the transport shouldn't change how
  human-readable the final UI is.

## Workflow

1. Confirm which endpoint(s) to build/update this session (ask if the user
   said "the API" generically without naming one).
2. Grep the doc for that endpoint's fields and grep the frontend for any
   existing partial implementation.
3. Write/extend the TypeScript type for the response shape, a small mapper
   function (raw response → display-friendly structure), and the React
   component/section that renders it per the design rules above.
4. Wire it into the existing panel/section for that system.
5. Verify: run the dev server or build, confirm no type errors, and where
   feasible do one live call to confirm real data renders correctly (not
   just mock data).
6. Report concisely what was added/changed — don't re-explain the whole
   frontend architecture each time.

## What NOT to do

- Don't invent fields not present in the documented response — if the doc
  doesn't specify a field's meaning, ask rather than guess at a label.
- Don't rebuild the whole dashboard shell if only one section changed.
- Don't leave a raw-data view as the default/primary display.