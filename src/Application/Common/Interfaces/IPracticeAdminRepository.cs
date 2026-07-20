using HekCoreApi.Contracts.Admin;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Block 2 domain group #16 - the actual write path onto Block 1's tenant registry
/// (ITenantRegistryService is read-only, ADR-001). Platform-admin scope only.
/// </summary>
public interface IPracticeAdminRepository
{
    Task<Practice> RegisterAsync(PracticeInput input, CancellationToken ct = default);

    Task<Practice?> GetAsync(string practiceId, CancellationToken ct = default);

    Task<Practice?> UpdateAsync(string practiceId, PracticeInput input, CancellationToken ct = default);
}
