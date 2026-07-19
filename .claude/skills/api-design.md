---
name: api-design
description: Defines REST API design standards, versioning, endpoint conventions, request/response contracts, error handling, pagination, validation, and documentation. Use whenever creating or modifying APIs.
---

# API Design Standards

## REST

Use resource-oriented endpoints.

Good

```
GET /patients
POST /patients
GET /patients/{id}
PUT /patients/{id}
DELETE /patients/{id}
```

Avoid

```
/GetPatients
/CreatePatient
```

---

# Versioning

Always version APIs.

Example

```
/api/v1/patients
```

---

# DTOs

Never expose EF entities.

Always use DTOs.

Separate

- Request DTO
- Response DTO

---

# Validation

Validate all incoming requests.

Use FluentValidation.

Return ProblemDetails for validation failures.

---

# Response Codes

200 OK

201 Created

204 No Content

400 Bad Request

401 Unauthorized

403 Forbidden

404 Not Found

409 Conflict

422 Validation Error

500 Internal Server Error

---

# Pagination

Large collections must support

- page
- pageSize

Return pagination metadata.

---

# Filtering

Support optional filtering.

Never create separate endpoints for every filter.

---

# Sorting

Support

sortBy

sortDirection

---

# Searching

Support keyword searching where appropriate.

---

# Error Handling

Use RFC7807 ProblemDetails.

Never return plain exception text.

---

# Authentication

JWT Authentication.

Refresh Token.

Authorization policies.

Role-based authorization.

Claims-based authorization.

---

# API Documentation

Every endpoint must include

Purpose

Request

Response

Status Codes

Authorization

Validation Rules

Business Rules

---

# Breaking Changes

Never introduce breaking API changes without versioning.

Document all API changes.