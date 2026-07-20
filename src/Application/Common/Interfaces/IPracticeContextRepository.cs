using HekCoreApi.Contracts.PracticeContext;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>COL GetSessionData, GetSurgeryData - practice/surgery/session context for the COL/Pegasus claiming flow.</summary>
public interface IPracticeContextRepository
{
    Task<PracticeSessionContext?> GetAsync(string practiceId, CancellationToken ct = default);
}
