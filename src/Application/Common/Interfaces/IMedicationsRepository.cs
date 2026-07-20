using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Medications;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>view distinguishes ERMS's Regular vs Prescribed lists (ERMS-BR-11) - one underlying data source, exposed explicitly rather than an internal boolean flag.</summary>
public interface IMedicationsRepository
{
    Task<IReadOnlyList<Medication>> GetAsync(OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession, string? view, CancellationToken ct = default);
}
