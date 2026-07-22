---
name: documentation-first-development
description: >
  Creates, maintains, and uses a complete living documentation system for any
  software project. Use for every new project, existing project analysis,
  feature implementation, bug fix, refactoring, or enhancement.

  Always consult project documentation before reading source code.
  Automatically update documentation after every approved change.
---

# Documentation First Development Skill

## Purpose

This skill minimizes token usage while maximizing project understanding.

Instead of analyzing the entire project on every task, maintain an always-updated
documentation repository describing architecture, business rules, implementation,
file locations, APIs, database structure, dependencies, and development history.

Documentation becomes the primary source of truth.

---

# Core Rules

## Rule 1

When starting a NEW project

Generate complete documentation before implementation begins.

---

## Rule 2

When working on an EXISTING project

Analyze the codebase once.

Create all documentation.

Never repeatedly analyze the whole project unless explicitly requested.

---

## Rule 3

Before every task

Read documentation first.

Only inspect source files that documentation identifies as relevant.

Do not scan unrelated modules.

---

## Rule 4

After every approved implementation

Automatically update every affected document.

Never leave documentation outdated.

---

## Rule 5

Documentation always reflects the current state of the software.

---

# Development Workflow

Every task follows this order.

Step 1

Read documentation.

↓

Step 2

Understand requested change.

↓

Step 3

Identify affected modules.

↓

Step 4

Open only required files.

↓

Step 5

Create implementation plan.

↓

Step 6

Present plan.

↓

Step 7

Implement after approval (or immediately if approvals are not required).

↓

Step 8

Update documentation.

↓

Step 9

Verify documentation consistency.

---

# Initial Project Analysis

Analyze once.

Document:

Project architecture

Solution structure

Modules

Features

Business rules

Database

Authentication

Authorization

API endpoints

Controllers

Services

Repositories

CQRS handlers

DTOs

Validators

Middleware

Filters

Configuration

External services

Dependencies

Logging

Caching

Background services

Message queues

Events

File storage

Tests

CI/CD

Deployment

Coding standards

Naming conventions

Folder structure

Future improvements

Known technical debt

---

# Documentation Structure

Create

docs/

    README.md

    DOCUMENT_INDEX.md

    ARCHITECTURE.md

    SOLUTION_STRUCTURE.md

    BUSINESS_RULES.md

    API_REFERENCE.md

    DATABASE.md

    AUTHENTICATION.md

    AUTHORIZATION.md

    DEPENDENCIES.md

    CONFIGURATION.md

    ERROR_HANDLING.md

    LOGGING.md

    BACKGROUND_SERVICES.md

    EXTERNAL_SERVICES.md

    FILE_STORAGE.md

    TESTING.md

    DEPLOYMENT.md

    SECURITY.md

    PERFORMANCE.md

    CHANGELOG.md

    ROADMAP.md

    TECHNICAL_DEBT.md

    GLOSSARY.md

    DEVELOPMENT_GUIDE.md

    CODE_INDEX.md

    MODULES/

        ModuleName.md

    FEATURES/

        FeatureName.md

---

# CODE_INDEX.md

Maintain a complete project code map.

Every important component must include

Purpose

Location

Responsibilities

Dependencies

Used By

Last Updated

Example

# Patient Controller

Path

src/Features/Patients/Controllers/PatientController.cs

Purpose

Patient API endpoints.

Uses

PatientService

PatientQueries

PatientCommands

Routes

GET /patients

POST /patients

PUT /patients/{id}

DELETE /patients/{id}

Related Files

PatientService.cs

PatientRepository.cs

PatientDto.cs

CreatePatientCommand.cs

PatientValidator.cs

Tests

PatientControllerTests.cs

---

Repeat this for

Controllers

Services

Repositories

Handlers

Validators

DTOs

Entities

Configurations

Middleware

Extensions

Utilities

Interfaces

Workers

Hosted Services

Events

Mappings

Database Context

Stored Procedures

Scripts

Views

Components

Pages

Frontend Screens

Shared Libraries

Everything.

---

# Module Documentation

Each module contains

Purpose

Responsibilities

Architecture

Business Rules

Dependencies

Database Tables

API Endpoints

DTOs

Commands

Queries

Events

Validation

Security

Known Issues

Future Improvements

Related Files

---

# Feature Documentation

Each feature includes

Business requirement

Technical design

Flow diagram (text)

Affected files

Database changes

API changes

Validation rules

Permissions

Test cases

Future enhancement ideas

---

# Architecture Documentation

Document

Architecture pattern

Clean Architecture

CQRS

DDD

MVC

Layer responsibilities

Dependency flow

Communication flow

Data flow

Folder structure

Design decisions

Tradeoffs

---

# API Documentation

For every endpoint

Route

Method

Request

Response

Validation

Authentication

Authorization

Business rules

Errors

Example request

Example response

Related files

---

# Database Documentation

Tables

Columns

Relationships

Indexes

Constraints

Stored procedures

Views

Triggers

Migration history

Business rules

---

# Business Rules

Document every discovered rule.

Examples

Appointment rules

Prescription rules

SMS rules

Patient rules

Billing rules

Validation rules

Workflow rules

Never lose business knowledge.

---

# Change Tracking

Maintain CHANGELOG.md

Each implementation records

Date

Feature

Reason

Files changed

Documentation updated

Breaking changes

Developer notes

---

# Future Enhancement Tracking

Maintain ROADMAP.md

Possible improvements

Known limitations

Future modules

Refactoring opportunities

Performance ideas

Security improvements

---

# Technical Debt

Maintain TECHNICAL_DEBT.md

Current shortcuts

Refactoring candidates

Known issues

Risk level

Priority

---

# Token Optimization Rules

Always use documentation first.

Never re-read the whole project if documentation exists.

Read only affected module documentation.

Read only required source files.

Reuse previous analysis.

Avoid duplicate explanations.

Avoid repeated architecture analysis.

Keep responses concise unless detailed output is requested.

Update documentation incrementally instead of regenerating it.

---

# Documentation Validation

Before finishing any task

Verify

Documentation updated

Code index updated

API docs updated

Business rules updated

Changelog updated

Roadmap updated if needed

No broken references

No outdated paths

---

# Quality Standards

Documentation must be

Accurate

Concise

Version controlled

Easy to navigate

Searchable

Developer friendly

Always synchronized with code

Never speculative

Based on actual implementation

---

# Never

Never delete documentation without reason.

Never leave documentation outdated.

Never modify unrelated documentation.

Never analyze the whole repository if documentation already provides sufficient context.

Never duplicate documentation.

---

# Goal

Maintain a living software knowledge base that allows future development by consulting documentation first, minimizing token usage while ensuring accurate, maintainable, and scalable software evolution.