---
name: docs-driven-workflow
description: Maintains a documentation-driven development workflow — a discoverable doc map, a decision log, and a completion checklist — for any repo/project that wants plan-first, module-isolated work with no assumed business rules. Auto-detects each project's actual documentation structure (a single running status file, a multi-file manifest, ADR logs, or none yet) instead of assuming one fixed layout, so the same skill works across different projects with different doc setups. Use before starting any feature/module work, and after any change that adds/changes entities, endpoints, jobs, business logic, or tests. Also use when asked to "update the docs", "sync documentation", "log this decision", "check the status/doc index", or "run the completion checklist" — even if the user doesn't name this skill directly and even if the project's docs look nothing like a previous project's.
---

# Documentation-Driven Workflow (generic)

This skill executes a documentation-driven workflow against whatever a specific project's
docs actually look like. It does not assume one repo's file names apply to every repo — the
first job, every time, is figuring out what this project uses.

## Step 0 — Discover this project's doc map (do this first, every time you're in a new project)

Check, in order, and stop at the first match:

1. **A project-specific skill already governs this.** Check the available skills list for one
   whose name or description names this specific project (e.g. something like
   `update-status-in-project-<x>`, or a description that references this project's path or
   status file directly). If one exists, defer to it instead of duplicating its job — it knows
   this project's exact file layout and rules better than a generic procedure can. Still verify
   the file it governs actually exists before relying on it (a quick directory listing), rather
   than trusting the skill's description alone.
2. **A root `CLAUDE.md` (or equivalent contributor guide)** that states a documentation policy.
   Follow it verbatim — it's authoritative for this repo, this skill is just the generic
   fallback for repos that don't have their own bespoke one.
3. **A single running status file** — `PROJECT_STATUS.md`, `STATUS.md`, `PROJECT_CONTEXT.md`,
   or similar. If present, this is almost always the single source of truth: read it in full
   before doing anything else, and update it (not a new file) when the work is done.
4. **A multi-file manifest pattern** — e.g. `docs/DOCUMENT_INDEX.md` (or similarly-named tree
   manifest) with linked per-feature anchor files, plus a separate decision log
   (`docs/DECISIONS.md` or similar), an architecture/implementation map (`CODEBASE_MAP.md` or
   similar), and possibly `docs/adr/` for formal architecture decision records.
5. **Nothing exists yet.** See "Bootstrapping" below rather than guessing at a structure.

Once found, treat that as the doc map for the rest of this session in this project — don't
re-derive it on every message, but don't trust it blindly forever either: if something in it
looks stale against what the code actually does, that's a signal to fix the doc, not silently
work around the mismatch (see Documentation rules, point 7).

## Planning workflow (before any implementation)

1. Read only the documentation relevant to the feature/module being touched — follow the doc
   map's links for that area, don't re-scan the whole project.
2. Produce an implementation plan.
3. Wait for explicit approval before writing any code.
4. If something is unclear, ask only functional clarification questions — not
   implementation-mechanics questions the plan should already answer.
5. Never assume business rules. If the project's spec/status docs don't cover something, ask
   rather than infer it.
6. Never begin coding before approval.

**Conflict resolution priority**, when two sources disagree — use this unless the project's own
`CLAUDE.md` (or equivalent) sets a different order, and report the conflict to the user before
implementing rather than guessing which source wins:

1. Explicit user instruction, or approved business rules in the project's spec/status doc
2. Formal functional specification (FR-/BR- numbered entries, or the closest equivalent)
3. Feature-level documentation (the manifest entry or status-log section for this feature)
4. Architecture/implementation documentation
5. Source code
6. Comments

## Module isolation

Read only the affected feature's documentation and whatever it links to. Read into dependent
modules only if the change genuinely crosses into them (e.g. a change that also triggers a
side-effect in another module). Don't scan the entire project when the doc map already tells
you which files/modules are relevant — that's the point of maintaining the map.

## Implementation rules

Unless the user explicitly asked for that specific action in this task, don't hand back manual
steps as something for them to run — execute builds, migrations, tests, and the app yourself
and verify the result directly, rather than asking the user to do it for you.

## Documentation rules (after implementation)

Run this whenever a change adds/changes an entity, endpoint, job, business rule, or test — in
the same session as the change, not as a deferred follow-up:

1. **Update only the affected documentation.** Don't touch unrelated entries or files.
2. **Update this feature's entry point** in whichever structure Step 0 found — a manifest tree
   entry with its anchor files, or the relevant status/phase/facts/change-log section of a
   single-file setup.
3. **Log the decision, if a real decision was made** (not just "implemented what was asked").
   Use the project's existing decision log if it has one; if it doesn't, add a dated entry to
   the status doc's change log instead of creating a new file for a single entry.
4. **Preserve existing documentation** — extend or merge, never rewrite wholesale.
5. **Never duplicate content** — cross-reference the architecture/implementation doc instead of
   restating it in a manifest entry or status section.
6. **Cross-reference check** — confirm links between the doc map's pieces resolve to real,
   current sections, not stale ones.
7. **If a doc's stated format/template ever conflicts with what the code actually does**, trust
   the code, fix the doc, and flag the discrepancy to the user rather than silently matching a
   stale template.

## Completion checklist

Before reporting a task done, state explicitly which of these hold — call out any that don't,
don't silently skip them:

- [ ] Implementation matches approved requirements
- [ ] Architecture/implementation doc updated (relevant section only)
- [ ] Feature-level doc entry added/updated (manifest + anchor files, or status-doc section)
- [ ] Decision logged, if one was made
- [ ] Cross-references valid
- [ ] Business rules documented/referenced where relevant
- [ ] APIs documented if changed
- [ ] Config changes documented
- [ ] Known limitations updated, if the project tracks those

## Bootstrapping a new project's doc structure

If Step 0 found nothing at all, don't invent a multi-file structure by default and don't stop
to ask what to call it — create `PROJECT_STATUS.md` at the project root (phase/status table, a
"confirmed facts" section, an "open decisions" section, and an append-only change log) and
proceed. This is lighter to maintain solo and scales down better than a multi-file manifest.
Only move to a multi-file manifest pattern if the user says they want per-feature doc files —
larger team, more features, or an explicit preference.