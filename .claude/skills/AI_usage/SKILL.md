---
name: ai-usage-log
description: >
  Maintains a running log of AI/Claude usage for a project at docs/ai_usage_log.md.
  Use this skill any time meaningful work is done for the user with Claude in a
  project context — writing code, editing files, making a decision, running a
  command, researching something, generating a document — and log it so there's
  a durable record of what AI did on this project. Trigger this proactively at
  the end of any substantive piece of work (not idle chat), even if the user
  doesn't explicitly say "log this" or mention "ai_usage_log" by name. Also
  trigger when the user asks to see, review, or summarize past AI activity on
  the project ("what have we done so far", "show me the usage log", "history of
  changes"). If docs/ai_usage_log.md doesn't exist yet in the project, create it
  with the header template below the first time this skill runs.
---

# AI Usage Log

Keeps a single markdown file, `docs/ai_usage_log.md`, as the source of truth for
what Claude has done on a project over time. The point is a lightweight,
human-readable audit trail — not a full transcript — so the user (or a
teammate) can later answer "what did the AI do here and why" without digging
through chat history.

## Where the file lives

`docs/ai_usage_log.md`, relative to the project's root (the folder the user is
currently working in). If there's no `docs/` folder yet, create one. If the
user is working outside any specific project folder, ask where the log should
live rather than guessing.

## When to log an entry

Log after finishing a unit of work worth remembering — not after every tiny
tool call. Good triggers:

- Code written, edited, or refactored
- A file created, deleted, or restructured
- A non-trivial decision made (e.g. "chose Postgres over Mongo because...")
- A command run that changed project state (migration, deploy, install)
- Research done and used to inform a change
- A document/report/spec generated

Skip logging for: pure Q&A that didn't change anything, clarifying questions,
or exploratory reads that didn't lead to an action.

## Entry format

Append (never overwrite) a new entry to the bottom of the file. If the file
doesn't exist, start it with this header:

```markdown
# AI Usage Log

Running record of AI-assisted work on this project.

---
```

Each entry follows this template:

```markdown
## YYYY-MM-DD HH:MM

**Task:** One-line summary of what was asked or accomplished.

**Actions:**
- Bullet per concrete action taken (files touched, commands run, decisions made)

**Files changed:** `path/one`, `path/two` (omit this line if nothing changed)

**Notes:** Any caveat, follow-up, or reasoning worth remembering (optional — omit if nothing to add)

---
```

Use the actual current date/time (check the system date if unsure — don't
guess). Keep entries terse; this is a log, not a report. One or two lines per
bullet is enough.

## Writing a new entry

1. Check if `docs/ai_usage_log.md` exists. If not, create it with the header above.
2. Read the last few lines to confirm formatting matches (in case the user or
   another tool edited it).
3. Append the new entry using the template, filling in real specifics — no
   placeholder text like "did some stuff." Be concrete: file names, what
   changed, why.
4. Don't ask the user for permission to log — just do it silently as part of
   wrapping up the work, the same way you'd naturally summarize what you did.

## When asked to review the log

If the user asks what's been done, read `docs/ai_usage_log.md` and answer from
its contents directly (most recent entries usually matter most) rather than
from memory of the conversation — the file is the source of truth across
sessions.