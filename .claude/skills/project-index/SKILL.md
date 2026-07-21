---
name: project-index
description: Uses the documentation index to minimize token usage. Always locate and read only the documentation relevant to the requested module before reading source code.
---

# Documentation First

Before reading code

1. Read PROJECT_INDEX.md

2. Locate the requested module.

3. Read only that module's documentation.

4. Read related ADRs only if referenced.

5. Read architecture.md only if required.

6. Read source code only for affected files.

7. Never scan unrelated modules.

---

# Token Optimization

Always prefer documentation over code.

Never analyze the whole repository.

Only open files needed for the task.

Avoid loading unrelated folders.

---

# Missing Documentation

If documentation is missing

Stop

Inform the user

Recommend documenting first

Do not invent business rules.

---

# Documentation Hierarchy

PROJECT_INDEX.md

↓

Module Documentation

↓

Architecture

↓

ADR

↓

Relevant Code

---

# After Implementation

Update only

- module documentation

- API documentation

- ADR (if required)

- change log

Do not rewrite unrelated documentation.