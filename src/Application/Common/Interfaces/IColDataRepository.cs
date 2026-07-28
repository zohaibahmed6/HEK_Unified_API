using System.Data;
using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from COL/Pegasus's `PHCO` DAL (`DAL/Pegasus/PHCO.cs`) plus the `[OnlineClaim].[uspInsertUpdateService]`
/// overload of `HSSDA.InsertUpdateService` (`HSSDA.cs:1064`). All on the same `ConnIndiciDB{practiceid}`
/// routing as ERMS (legacy COLController lives in-process with ERMS's APIController and shares its DAL).
/// </summary>
public interface IColDataRepository
{
    /// <summary>`PHCO.GetCurrentPatientData` - `[OnlineClaim].[uspGetPatientData]`.</summary>
    Task<DataTable> GetCurrentPatientDataAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default);

    /// <summary>`PHCO.GetProviderData` - `[OnlineClaim].[uspGetProvider]`.</summary>
    Task<DataTable> GetProviderDataAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default);

    /// <summary>`PHCO.GetSessionData` (`PHCO.cs:57`) - real legacy BUG preserved: the stored-procedure name is an EMPTY STRING, so the call always fails and the SQL error becomes the response text.</summary>
    Task<DataTable> GetSessionDataAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default);

    /// <summary>`PHCO.GetDiagnosisData` - `[OnlineClaim].[uspGetConditions]`; NOTE: legacy COL passes `pmsOrder` raw (no ToUpper, unlike ERMS).</summary>
    Task<DataTable> GetDiagnosisDataAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`PHCO.GetSurgeryData` - `[OnlineClaim].[uspGetSurgeryData]`; `@pEncounterId` always sent, `@pLocationId` only when non-blank.</summary>
    Task<DataTable> GetSurgeryDataAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? locationId, string? encounterId, CancellationToken ct = default);

    /// <summary>`HSSDA.InsertUpdateService` 16-arg overload → `[OnlineClaim].[uspInsertUpdateService]`; masterServiceName is the literal "COL", locationId always blank from COL (sent as DBNull). Throws on proc failure (message → response text).</summary>
    Task<long> SaveInvoiceAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? accountHolderId, string? encounterId, string? serviceName, string? serviceCode,
        string? fee, string? description, string? payee, string? serviceProvider, string? serviceProviderType, string? serviceDate, string? pegasusReference, string? claimShortCode, CancellationToken ct = default);
}
