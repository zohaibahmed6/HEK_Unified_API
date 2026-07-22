using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Documents;

/// <summary>
/// Unified document/attachment metadata shape spanning HISO/KARO/ERMS (FR-DOC-03: direction/type kept
/// as first-class fields rather than merged away, per the Contract Design doc's existing decision).
/// Deliberately metadata-only for this first pass - no binary content field - to keep the canonical
/// list endpoint lightweight; content retrieval is a separate, heavier follow-up.
///
/// First-pass scope, not full coverage yet (flagged, not silently pretended complete):
/// - KARO: `documents` op only (`patientattachment` is a separate real KARO operation, not yet included).
/// - ERMS: `GetOtherDocs` non-discharge variant only (`isReferral: false`) - the discharge-summary
///   variant is the same procedure with a different flag, not yet wired up as a second source.
/// - HISO: `Patient_Attachment` concept only (`Patient_OutgoingLetter` is a second real, confirmed
///   concept, not yet included).
/// - COL: no document operation exists in real COL source - not supported.
/// </summary>
public sealed record DocumentCanonical(
    string? DocumentId,
    string Name,
    string? Subject,
    string? DocumentType,
    string? DateCreated,
    OriginScope Source);
