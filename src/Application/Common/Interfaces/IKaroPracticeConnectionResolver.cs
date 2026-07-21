namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// KARO/HSS's real connection-routing mechanism (confirmed from
/// legacy-reference/hsswebapi/DevLocal/DAL/South/HSSDA.cs, `InsertAndValidateToken`):
/// `ConfigurationManager.ConnectionStrings["ConnIndiciDB" + practiceid]` - the practice suffix
/// (parsed out of the request's own `encounterId`, e.g. "_485", "_LOCAL") is concatenated directly
/// onto the base connection-string name. This is a fundamentally different routing model than HISO's
/// tenant-registry-by-PracticeId (<see cref="ILegacyPracticeConnectionResolver"/>) - KARO has no
/// separate practice registry, the suffix from the caller's own request selects the target.
/// </summary>
public interface IKaroPracticeConnectionResolver
{
    /// <summary>
    /// Resolves the real connection string for `practiceSuffix` (e.g. "", "_485", "_LOCAL").
    /// Throws <see cref="Domain.Exceptions.NotFoundException"/> if no `Karo:DbCredentials:ConnIndiciDB{practiceSuffix}`
    /// secret is configured for this suffix - never falls back to a different target silently.
    /// </summary>
    Task<string> ResolveAsync(string practiceSuffix, CancellationToken ct = default);
}
