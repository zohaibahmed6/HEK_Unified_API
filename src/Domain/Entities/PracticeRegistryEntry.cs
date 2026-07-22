namespace HekCoreApi.Domain.Entities;

/// <summary>
/// One row per (PracticeId, PracticeCode, Environment) routing target in the tenant/practice
/// registry (ADR-001). Rebuilt 2026-07-22 with a real surrogate key: the real KARO/ERMS
/// <c>EncounterId</c> format (<c>{encId}__{practiceId}__{practiceCode}__{environment}</c>) encodes
/// three distinct routing facts, not one flat practiceId - HISO and every other Block 2 repository
/// that only ever had a flat practiceId use a fixed sentinel value ("-", RoutingContext.Unscoped in
/// the Application layer) for <see cref="PracticeCode"/>/<see cref="Environment"/> and keep resolving by
/// <see cref="PracticeId"/> alone.
/// </summary>
public sealed class PracticeRegistryEntry
{
    public int Id { get; set; }

    public required string PracticeId { get; set; }

    public required string PracticeCode { get; set; }

    /// <summary>Server/cluster selector (e.g. "SOUTH") - decides which physical DB a practice routes to.</summary>
    public required string Environment { get; set; }

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

    /// <summary>Local server time, not UTC - deliberate deviation from the rest of the codebase's UTC convention, scoped to this table only.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
