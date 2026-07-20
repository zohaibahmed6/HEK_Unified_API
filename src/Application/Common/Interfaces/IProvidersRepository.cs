using HekCoreApi.Contracts.Providers;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>KARO GetProvider; ERMS GetRegisteredPractitioners (ERMS-BR-17); COL GetProviderData - one canonical implementation.</summary>
public interface IProvidersRepository
{
    Task<IReadOnlyList<Provider>> GetAsync(string practiceId, string? practiceLocationId, CancellationToken ct = default);
}
