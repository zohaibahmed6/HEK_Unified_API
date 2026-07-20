namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Resolves a full ADO.NET connection string for a practice's routed legacy database (ADR-001).
/// Every Block 2 domain repository goes through this rather than building its own connection
/// string, so tenant routing stays centralized in exactly one component (FR-PAT-02).
/// </summary>
public interface ILegacyPracticeConnectionResolver
{
    /// <summary>Throws <see cref="Domain.Exceptions.NotFoundException"/> if <paramref name="practiceId"/> is not a registered practice.</summary>
    Task<string> ResolveAsync(string practiceId, CancellationToken ct = default);
}
