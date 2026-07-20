using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Observations;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Application.Common.Interfaces;

public interface IObservationsRepository
{
    Task<IReadOnlyList<Observation>> GetAsync(OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession, string? conceptId, CancellationToken ct = default);

    Task<Observation> SaveAsync(int patientId, int encounterId, string practiceId, ObservationInput input, CancellationToken ct = default);
}
