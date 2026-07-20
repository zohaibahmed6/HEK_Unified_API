using HekCoreApi.Contracts.Screening;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// SaveAsync is the real, working implementation replacing legacy SaveScreeningCode, which
/// performed no token validation and always reported fake success without persisting (KARO-BR-06) -
/// per stakeholder Decision 5/FR-AUTH-04, not a reproduction of that behavior.
/// </summary>
public interface IScreeningRepository
{
    Task<IReadOnlyList<ScreeningCode>> GetCodesAsync(string practiceId, CancellationToken ct = default);

    Task<ScreeningCodeResult> SaveAsync(int patientId, int encounterId, string practiceId, ScreeningCodeInput input, CancellationToken ct = default);
}
