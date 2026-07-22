namespace HekCoreApi.Domain.Entities;

/// <summary>
/// A row in the central HISO session registry (HekTenantRegistry.HisoSessions) - deliberately mirrors
/// <see cref="PracticeRegistryEntry"/> field-for-field, plus <see cref="SessionGuid"/>. Each row is
/// self-sufficient: it carries its own <see cref="DbServerHost"/>/<see cref="DbName"/> directly, so
/// resolving a HISO session never needs a secondary lookup into <c>Practices</c> - unlike ERMS/KARO,
/// which continue routing through <c>Practices</c> unchanged.
/// </summary>
public sealed class HisoSessionRegistryEntry
{
    public int Id { get; set; }

    public Guid SessionGuid { get; set; }

    public required string PracticeId { get; set; }

    public required string PracticeCode { get; set; }

    public required string Environment { get; set; }

    public required string PracticeName { get; set; }

    /// <summary>Always "Hiso" for rows in this table - kept for shape-parity with <see cref="PracticeRegistryEntry"/>.</summary>
    public required string SourceSystem { get; set; }

    public required string DbServerHost { get; set; }

    public required string DbName { get; set; }

    public bool RowLevelSecurityEnabled { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Local server time, not UTC - matches <see cref="PracticeRegistryEntry"/>'s convention. Used for the ADR-004 12-hour expiry check.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
