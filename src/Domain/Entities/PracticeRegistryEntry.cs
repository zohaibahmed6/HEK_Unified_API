namespace HekCoreApi.Domain.Entities;

/// <summary>
/// One row per practice in the new tenant/practice registry database (ADR-001), replacing HISO's
/// fixed 4-connection model and KARO/ERMS's per-practice Web.config connection-string convention.
///
/// INFERRED SCHEMA - flagged, not a confirmed fact: no source document (SRS/EAD/ADR log/Contract
/// Design doc) gives a literal column list for this registry, only ADR-001's prose description
/// ("one row per practice with which physical database server it lives on"). This is a reasonable
/// minimal design pending confirmation - see PROJECT_STATUS.md Block 1 change-log entry.
/// </summary>
public sealed class PracticeRegistryEntry
{
    /// <summary>Matches the practiceId used in TokenRequest/ResourceScope and HISO's PracticeID column.</summary>
    public required string PracticeId { get; set; }

    public required string PracticeName { get; set; }

    /// <summary>Which legacy system originates this practice's data (Hiso/Karo/Erms/Col).</summary>
    public required string SourceSystem { get; set; }

    public required string DbServerHost { get; set; }

    public required string DbName { get; set; }

    /// <summary>
    /// ADR-001/ADR-008 off-by-default row-level-security toggle. The column exists now so RLS can be
    /// switched on later, per practice, with no schema migration - RLS itself is not implemented in
    /// Block 1 (deferred, per ADR-001).
    /// </summary>
    public bool RowLevelSecurityEnabled { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
