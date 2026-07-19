---
name: coding-standards
description: Enforces coding conventions, SOLID principles, naming standards, async usage, null safety, and maintainable code practices. Use before writing or reviewing any C# code.
---

# Coding Standards

## Principles

Always follow

- SOLID
- DRY
- KISS
- YAGNI

---

# Naming

Classes

PascalCase

Methods

PascalCase

Properties

PascalCase

Private fields

_camelCase

Interfaces

IPatientService

---

# Methods

Keep methods small.

Single responsibility.

Avoid methods over 50 lines.

Extract reusable logic.

---

# Classes

Prefer focused classes.

Avoid God Objects.

---

# Async

Use async/await.

Never block async code.

Avoid .Result

Avoid .Wait()

---

# Nullability

Use nullable reference types.

Never disable nullable warnings.

---

# Dependency Injection

Constructor injection only.

---

# Magic Values

Avoid hardcoded strings.

Use constants or configuration.

---

# Configuration

Use IOptions pattern.

Do not hardcode settings.

---

# Comments

Code should be self-explanatory.

Comment only complex business rules.

---

# XML Documentation

Public APIs should include XML documentation.

---

# Exceptions

Throw meaningful exceptions.

Do not swallow exceptions.

---

# Clean Code

Prefer readability over cleverness.

Reduce nesting.

Use guard clauses.

Return early.

---

# Refactoring

Always improve nearby code if it increases readability without changing behavior.

Never perform unrelated refactoring.