using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Recalls;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Recalls;

/// <summary>KARO-only canonical resource (2026-07-23) - reuses <see cref="IKaroDataRepository.GetRecallsAsync"/>, no new DB code.</summary>
public sealed class CanonicalRecallsRepository : ICanonicalRecallsRepository
{
    private readonly IKaroDataRepository _karoRepository;

    public CanonicalRecallsRepository(IKaroDataRepository karoRepository)
    {
        _karoRepository = karoRepository;
    }

    public async Task<IReadOnlyList<RecallCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var recalls = await _karoRepository.GetRecallsAsync(routing.PracticeId, patientId, ct);
        return recalls.Select(r => new RecallCanonical(r.CategoryId, r.Priority, r.DueDate, r.Reason, r.Notes, OriginScope.Karo)).ToList();
    }
}
