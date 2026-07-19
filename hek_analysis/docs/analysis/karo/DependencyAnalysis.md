# KARO — Dependency Analysis (External Systems)

**Summary:** KARO itself has a narrow set of live external dependencies (its PMS/DMS SQL Server databases and an opaque AWS-backed document library), but it ships inside a shared DAL library that also carries code for many sibling integrations (GP2GP, MHNHL7, BPAC/BPI, Pegasus, Procare/Procon, Screening, MHNAppointment) that are not reachable from KARO's own controller.

## Findings

### Live dependencies (reachable from `APIController.cs` → `HSSDA.cs`)
| Dependency | Type | What it does (evidence) | Reachability |
|---|---|---|---|
| Indici PMS database (`ConnIndiciDB[+practice suffix]`) | SQL Server, stored procedures under `[HSS]` schema | Primary data store for demographics, conditions, consult notes, medications, labs, observations, recalls, invoices, tokens | Live, every endpoint |
| DMS database (`ConnDMSDB[+practice suffix]`) | SQL Server, `[dbo]` schema | Binary document storage (`uspDocumentSave`, `uspDocumentDelete`, `uspUpdateExistingDoc`) | Live, `SaveDocument`/`GetDocuments`/`GetPatientAttachment` |
| `AWSDoc.dll` (`AWSDoc.IndiciDMS`) | Precompiled external .NET library, no source in repo | AWS-backed document storage/retrieval path used when a practice has "AWS enabled" (`DAL\South\HSSDA.cs` lines 280, 306, 331: `CheckAWSIsEnabled`, `GetDocumentStatusFromIndici`, `DocumentGetByDocumentKeyJsonResult`) | Live, conditionally, for AWS-migrated practices only, in `GetDocuments` |

> Unable to verify from available source what AWS service(s) `AWSDoc.dll` actually calls (S3, or a wrapper API) — only its call signature and usage context are visible.

### Dead-for-KARO dependencies (present in shared `DAL.dll`, not called by `APIController.cs`)
These are documented here because they ship in the exact same compiled artifact KARO depends on, and because a future engineer skimming the DAL folder structure could reasonably (but incorrectly) assume KARO uses them.

| Module | Connection string(s) | What it appears to integrate with |
|---|---|---|
| `BPAC\BPACDA.cs` | `ConnMHNBPAC` (inferred name) | A parallel/older PMS-facing integration: ACC18 claims, demographic/diagnosis/consult-note retrieval via an `[Appointment]` schema — conceptually overlapping with KARO's own `[HSS]` schema calls but a distinct code path |
| `BPI\BPIDA.cs` | `ConnBPI` | Bulk patient extract / National Immunisation Register (NIR)-style batch integration (`uspNIRINLINE`, `uspPatientExtract`) |
| `DMS\DMSDA.cs` | `ConnMHNPMS`, `ConnMHNDMS` | HL7 inbox handling and document storage for a separate "MHN" system; contains raw string-concatenated SQL (see `SecurityAnalysis.md`) |
| `GP2GP\PIDAO.cs` | connection string name not declared in KARO's `Web.config` | UK NHS GP2GP-style clinical record transfer (patient demography, author/custodian metadata) — a message-format integration, not used by KARO |
| `MHNAppointment\MHNAppointmentDA.cs` | `ConnMHNDataMigration` | Appointment/HL7 data migration tooling with its own authentication procedures, unrelated to KARO's token model |
| `MHNHL7\DBMessages.cs` | `ConnMHNPMS`/`ConnMHN` | HL7 message header/prescription message handling, screening template configuration; also contains raw SQL string concatenation |
| `Pegasus\PHCO.cs` | `ConnIndiciDB*` (same DB as KARO) | A **separate "online claims" API** reading the same PMS database via an `[OnlineClaim]` schema — a sibling system, not KARO |
| `Procare\ProcareApiDA.cs`, `Procon\ProconApiDA.cs` | `ConnIndiciDB*` (same DB as KARO) | **Near-duplicate implementations** of much of KARO's own functionality (reusing several of the same `[HSS]` schema stored procedures: `uspGetACC45`, `uspGetAllergies`, `uspGetLabResults`, `uspGetMeasurement`, `uspGetDocResults`) but with their own token validation (`[Auth].[uspProcareValidateToken]`) — almost certainly other portal-vendor integrations built from the same original codebase as KARO |
| `Screening\ScreeningDAL.cs` | connection string name not declared in KARO's `Web.config` | Screening template configuration and JSON-based screening data capture |
| `UI\GUIDA.cs` | uses an older `Hashtable`-based parameter pattern | Small admin/config UI support class (message header/field-value lookups) |

### Message queues / SMTP / other external APIs
- **No** message queue client libraries (`MSMQ`, RabbitMQ, Azure Service Bus, Kafka, etc.) were found anywhere in `packages.config` or the source.
- **No** SMTP/email sending code (`SmtpClient`) was found anywhere in the DAL or HSSWebAPI projects (`grep -rn "SmtpClient" ...` returned no matches).
- **No** outbound `HttpClient`/`WebRequest`/`WebClient` calls were found in the DAL or controller code (`grep -rn "HttpClient|WebRequest|WebClient" ...` returned no matches) — the only "external API" style dependency is the precompiled `AWSDoc.dll`.
- HL7 messaging (`MHNHL7\DBMessages.cs`) appears to be **database-mediated** (inbound HL7 messages are presumably parsed and inserted by an out-of-repo process, and this DAL class reads/writes the resulting tables) rather than KARO/DAL directly speaking the HL7 protocol over a network transport — no HL7 parsing library (e.g., NHapi) reference was found.
> Unable to verify from available source how HL7 messages physically arrive into the `Config`/`dbo` tables referenced by `DBMessages.cs` — that ingestion mechanism is outside this repository.

### Windows Services / background workers / cron
No Windows Service project, Hangfire, Quartz.NET, or scheduled-task code was found in this repository. All modules are libraries called synchronously from the web request pipeline (or, presumably, from other applications in the wider `MHNPHMP-Integration` solution not included here).

## Evidence
Citations inline above; primary files across `E:\NZTFS\hsswebapi\DevLocal\DAL\*`.

## Risks
- The "near-duplicate" Procare/Procon/Pegasus integrations sharing KARO's `[HSS]` schema stored procedures mean that any schema or stored-procedure change made "for KARO" during the unified-platform migration could silently break these other, currently out-of-scope systems — and vice versa, since this analysis has no visibility into their own controllers/hosts (they may live in the same `MHNPHMP-Integration` solution outside this repo).
- The unexplained `AWSDoc.dll` binary dependency is a black box; if the unified platform needs to replicate AWS-document-store behavior for AWS-enabled practices, its logic must be re-derived (from its behavior via testing) or sourced from wherever its original project lives.

## Recommendations
- Before migration, confirm with the client whether Procare, Procon, Pegasus, BPAC, BPI, GP2GP, MHNHL7, MHNAppointment, Screening, and UI DAL modules belong to systems that are in scope for the unified platform (perhaps as part of HISO/ERMS, which are being analyzed in parallel) or are genuinely out of scope legacy code that can be retired.
- Locate the source for `AWSDoc.dll` if it exists anywhere in the broader `MHNPHMP-Integration` solution, since it is a hard dependency for KARO's AWS-enabled document retrieval path.
