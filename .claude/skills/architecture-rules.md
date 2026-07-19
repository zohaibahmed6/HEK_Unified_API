---
name: architecture-rules
description: Enforces Clean Architecture, Domain-Driven Design principles where appropriate, dependency rules, and project structure for all implementations. Use before implementing any new feature, refactor, endpoint, service, background job, integration, or database change.
---

# Architecture Rules

## Primary Goal

Maintain a scalable, maintainable, testable, and modular codebase.

Never sacrifice architecture for short-term convenience.

---

# Architecture

Always follow Clean Architecture.

```
Presentation
    ↓
Application
    ↓
Domain
    ↓
Infrastructure
```

Dependencies only point inward.

Never create circular dependencies.

---

# Layer Responsibilities

## Presentation

Contains:

- Controllers
- Minimal APIs
- Authentication
- Authorization
- API Versioning
- Filters
- Middleware

Must never contain business logic.

Controllers should be thin.

Controllers should delegate immediately to Application.

---

## Application

Contains:

- Use Cases
- CQRS Commands
- CQRS Queries
- DTOs
- Validators
- Interfaces
- Business workflows

Contains orchestration only.

No EF Core.

No SQL.

No HTTP calls.

No infrastructure implementation.

---

## Domain

Contains:

- Entities
- Value Objects
- Domain Services
- Domain Events
- Business Rules
- Enums

Domain must have zero dependencies on Infrastructure.

---

## Infrastructure

Contains

- EF Core
- SQL
- External APIs
- Email
- SMS
- Storage
- Repositories
- Identity Providers

Infrastructure implements interfaces defined in Application.

---

# Dependency Rules

Allowed

Presentation → Application

Application → Domain

Infrastructure → Application

Infrastructure → Domain

Forbidden

Presentation → Infrastructure

Presentation → Domain

Application → Infrastructure

Domain → Any Layer

---

# Feature Organization

Prefer Feature Folders.

Example

```
Features/

Patients/

Commands/

Queries/

Dtos/

Validators/

Services/

Controllers/
```

Avoid large shared folders.

---

# Business Logic

Business rules belong inside Application or Domain.

Never inside

- Controllers
- Repositories
- DbContext
- Middleware

---

# Repository Rules

Repositories expose only persistence.

Repositories never contain business rules.

Repositories return domain entities whenever practical.

---

# Dependency Injection

Register dependencies only inside Program.cs.

Never manually instantiate services.

Always use constructor injection.

Never use Service Locator.

---

# Error Handling

Use centralized exception middleware.

Never use try/catch everywhere.

Only catch exceptions when recovery is possible.

---

# Logging

Use structured logging.

Never log secrets.

Never log passwords.

Never log JWT tokens.

---

# Documentation

Any architectural decision affecting multiple modules must be recorded in an ADR.

Update architecture documentation whenever the architecture changes.