# PROJECT MEMORY SKILL

## Purpose

You are the permanent project memory for this repository.

Your responsibility is to continuously maintain an accurate, lightweight, searchable knowledge base of the project so that neither the user nor Claude needs to rely on conversation memory.

This skill must work for ANY software project regardless of language, framework or architecture (.NET, Java, Node.js, Python, Go, React, Angular, Vue, PHP, Mobile, Desktop, Infrastructure, etc.).

Your documentation becomes the project's long-term memory.

---

# PRIMARY OBJECTIVES

1. Never lose project knowledge.

2. Keep documentation synchronized with the source code.

3. Log every meaningful change.

4. Minimize future token usage.

5. Allow any future Claude session to understand the project quickly.

6. Make documentation the single source of truth.

7. Avoid scanning the entire project whenever possible.

---

# FIRST TIME EXECUTION

If this skill is added to an existing project and documentation does not already exist:

Perform a complete project discovery.

Analyze:

• Folder structure
• Technologies
• Frameworks
• Architecture
• APIs
• Database
• Dependencies
• Configuration
• Authentication
• Authorization
• External services
• Background jobs
• Infrastructure
• Build system
• Tests
• CI/CD
• Modules
• Features
• Design patterns
• Coding conventions
• Existing documentation
• Git history (if available)

Then generate the documentation structure.

Do NOT ask the user to manually create documentation.

Create everything automatically.

---

# REQUIRED DOCUMENT STRUCTURE

docs/

INDEX.md

PROJECT_MASTER.md

TODAY.md

CHANGELOG.md

TODO.md

DECISIONS.md

KNOWN_ISSUES.md

CODE_INDEX.md

API_INDEX.md

DATABASE.md

DEPENDENCIES.md

ROADMAP.md

archive/

Daily logs

---

# INDEX.md

This is always the entry point.

Contains:

Project summary

Document list

Read order

Current project status

Last update

Never place detailed information here.

---

# PROJECT_MASTER.md

This is the permanent memory.

Store only long-term information.

Include:

Project purpose

Architecture

Modules

Major features

Authentication

Authorization

Technology stack

Folder structure

External integrations

Coding standards

Business rules

Important workflows

Deployment overview

Current project state

Major milestones

Never place daily work here.

Never duplicate daily logs.

---

# TODAY.md

Represents ONLY today's work.

Contains:

Current objective

Completed tasks

Files modified

Important code changes

Problems encountered

Solutions

Current blockers

Testing performed

Next steps

Open questions

At the beginning of a new working day:

Move previous TODAY.md into:

docs/archive/YYYY-MM-DD.md

Create a fresh TODAY.md

Never lose historical logs.

---

# CHANGELOG.md

Contains completed features only.

Ordered newest first.

Each entry contains:

Date

Feature

Summary

Files affected (high level)

Impact

No implementation details.

---

# TODO.md

Contains:

Pending work

Future enhancements

Technical debt

Refactoring tasks

Priority

Status

Owner (if known)

Automatically remove completed tasks.

Automatically add new pending work.

---

# DECISIONS.md

Store architectural decisions.

Each decision includes:

Date

Problem

Decision

Reason

Alternatives

Consequences

Never remove historical decisions.

---

# KNOWN_ISSUES.md

Track:

Known bugs

Temporary workarounds

Limitations

Performance concerns

Security concerns

Future fixes

---

# CODE_INDEX.md

Maintain a searchable map.

For every major feature include:

Feature

Location

Main files

Purpose

Dependencies

Related APIs

Related database tables

Keep concise.

---

# API_INDEX.md

Automatically maintain:

Endpoint

Purpose

Authentication

Input

Output

Dependencies

Notes

Do not duplicate Swagger.

Provide a high-level index.

---

# DATABASE.md

Document:

Tables

Views

Stored procedures

Relationships

Important business rules

Migration history (high level)

---

# DEPENDENCIES.md

Document:

Frameworks

Packages

SDKs

External APIs

Cloud services

Version notes

---

# ROADMAP.md

Contains:

Upcoming milestones

Large planned work

Future architecture

Known future improvements

---

# DAILY ARCHIVE

Every working day must be preserved.

Example:

archive/

2026-07-28.md

2026-07-29.md

2026-07-30.md

Never overwrite history.

---

# DURING EVERY TASK

Before making changes:

Read only:

INDEX.md

PROJECT_MASTER.md

TODAY.md

Then read ONLY documentation related to the requested task.

Never read everything unless absolutely required.

---

# AFTER EVERY IMPLEMENTATION

Automatically perform:

Update TODAY.md

Update PROJECT_MASTER.md if permanent information changed

Update CHANGELOG.md if a feature completed

Update TODO.md

Update DECISIONS.md if architecture changed

Update KNOWN_ISSUES.md if required

Update CODE_INDEX.md

Update API_INDEX.md if APIs changed

Update DATABASE.md if schema changed

Update DEPENDENCIES.md if dependencies changed

Update ROADMAP.md if future work changed

Never wait for the user to request documentation updates.

Documentation maintenance is part of every implementation.

---

# SYNCHRONIZATION RULES

If code changes:

Update documentation.

If documentation is outdated:

Correct documentation.

If documentation conflicts with code:

Code is the source of truth.

Synchronize immediately.

---

# RECONSTRUCTION MODE

If documentation is missing:

Analyze project.

Reconstruct documentation.

Continue normal workflow.

Do not ask permission.

---

# TOKEN OPTIMIZATION

Never scan the full project for every request.

Preferred workflow:

Read INDEX

↓

Read PROJECT_MASTER

↓

Read TODAY

↓

Read only affected documentation

↓

Read only affected code

↓

Implement changes

↓

Update documentation

This minimizes tokens.

---

# HISTORY RULES

Never delete history.

Never rewrite history.

Never remove completed milestones.

Never remove archived logs.

Always preserve previous decisions.

Always preserve changelog.

Always preserve project evolution.

---

# WRITING RULES

Documentation must be:

Short

Technical

Searchable

Accurate

Consistent

Avoid unnecessary explanations.

Avoid duplicated information.

Use headings.

Use bullet points where appropriate.

Keep documents easy to scan.

---

# WHEN NEW FEATURES ARE ADDED

Automatically:

Document feature

Update indexes

Update roadmap

Update project state

Update changelog

Update code index

Update APIs

Update database if needed

Update dependencies if needed

---

# WHEN BUGS ARE FIXED

Automatically:

Update TODAY

Update CHANGELOG

Update KNOWN_ISSUES

Update CODE_INDEX if affected

---

# WHEN REFACTORING

Automatically record:

Reason

Files affected

Architecture impact

Performance impact

Breaking changes

---

# PROJECT COMPLETION

When all planned work is complete:

Update PROJECT_MASTER

Mark roadmap items complete

Finalize TODO

Create final changelog entry

Ensure every document is synchronized

---

# GOLDEN RULES

Always keep documentation alive.

Documentation is mandatory.

Documentation is never optional.

Documentation is part of implementation.

Never finish a task without updating documentation.

Never depend on conversation memory.

The documentation is the project's permanent memory.

Always optimize for future maintainability, discoverability, and minimum token usage.