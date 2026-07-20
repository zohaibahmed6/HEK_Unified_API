using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Acc45;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Canonical internal REST equivalents of HISO's FormSessionService.svc SOAP contract (Contract
/// Design doc Section 4.9) - not directly exposed to HISO's existing consumer; the (not-yet-built)
/// SOAP edge adapter is the only intended caller, using a token minted via Block 1's
/// HisoSessionResolver. sessionKey is used as a resource identifier for form-data lookups, distinct
/// from the token's own claims.
/// </summary>
public interface IAcc45Repository
{
    Task<VersionInfo> GetVersionAsync(CancellationToken ct = default);

    Task<DeliveryOptions> GetDeliveryOptionsAsync(HealthLinkSession session, CancellationToken ct = default);

    /// <summary>mode reflects the practice's IsDynamic setting. Legacy "static" mode was an empty stub (HISO-BR-17) - implemented for real per stakeholder Decision 4, not a no-op.</summary>
    Task<FormData> GetFormsAsync(Guid sessionKey, HealthLinkSession session, string mode, CancellationToken ct = default);

    Task<FormData?> GetFormAsync(Guid sessionKey, string formInstanceId, HealthLinkSession session, CancellationToken ct = default);

    /// <summary>HISO-BR-12 sequencing: render view to DMS, then persist ACC45 definition/diagnosis/referral, DMS GUID linked.</summary>
    Task<FormData> SaveFormAsync(Guid sessionKey, string formInstanceId, HealthLinkSession session, FormSaveInput input, CancellationToken ct = default);

    /// <summary>save/addTask/addInvoice/launchForm - the latter two implemented for real per Decision 4, not silent no-ops.</summary>
    Task<FormActionResult> DispatchActionAsync(Guid sessionKey, HealthLinkSession session, FormActionInput input, CancellationToken ct = default);
}
