using System.Data;
using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from ERMS's `HSSDA` read calls (`DAL/South/HSSDA.cs`) used by the Get* operations -
/// consolidated into one repository (same precedent as KARO's `IKaroDataRepository`, matching
/// legacy's own single shared `HSSDA` static class). Each method returns the raw pipe-delimited
/// DataTable; `ErmsDataTableMapper` in the adapter layer does the HISO mapping.
/// Date parameters follow legacy exactly: `DateTime.MinValue` means "omit the SQL parameter".
/// `routingContext` selects the tenant-registry route (ADR-001);
/// `practiceSuffix`/`practiceSuffixNumeric` remain for the separate DMS connection and AWS branching.
/// </summary>
public interface IErmsDataRepository
{
    /// <summary>`HSSDA.GetMeasurement` (`HSSDA.cs:520`) - `[HSS].[uspGetMeasurement]`. Legacy passes an `encounterId` arg it never uses - omitted here.</summary>
    Task<DataTable> GetMeasurementAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetSmokingStatus` (`HSSDA.cs:907`) - `[HSS].[uspGetSmokingStatus]`.</summary>
    Task<DataTable> GetSmokingStatusAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetProvider` (`HSSDA.cs:738`) - `[HSS].[uspGetProvider]`; userId/locationId/encounterId params only sent when non-blank.</summary>
    Task<DataTable> GetProviderAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? userId, string? locationId, string? encounterId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetNextOfKin` (`HSSDA.cs:608`) - `[HSS].[uspGetNextOfKin]`.</summary>
    Task<DataTable> GetNextOfKinAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetRegisteredPractitioners` (`HSSDA.cs:862`) - `[HSS].[uspGetRegisteredPractitioners]`; locationId only sent when non-blank.</summary>
    Task<DataTable> GetRegisteredPractitionersAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? locationId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetACC45` (`HSSDA.cs:143`) - `[HSS].[uspGetACC45]`.</summary>
    Task<DataTable> GetAcc45Async(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetConditions` (`HSSDA.cs:177`) - `[HSS].[uspGetConditions]` (legacy `GetClassifications` operation).</summary>
    Task<DataTable> GetConditionsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetConsultNotes` (`HSSDA.cs:210`) - `[HSS].[uspGetConsultNotes]`.</summary>
    Task<DataTable> GetConsultNotesAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetMedicalAllergies` (`HSSDA.cs:541`) - real proc is `[HSS].[uspGetAllergies]`.</summary>
    Task<DataTable> GetMedicalAllergiesAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetMedications` (`HSSDA.cs:575`) - `[HSS].[uspGetMedications]`; `@pIsLongTerm` false=Prescribed / true=Regular, `@pShowStop` always false.</summary>
    Task<DataTable> GetMedicationsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, bool isLongTerm, CancellationToken ct = default);

    /// <summary>`HSSDA.GetLabs` (`HSSDA.cs:491`) - `[HSS].[uspGetLabs]`.</summary>
    Task<DataTable> GetLabsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetLabResults` (`HSSDA.cs:461`) - `[HSS].[uspGetLabResults]`; both params only sent when non-blank.</summary>
    Task<DataTable> GetLabResultsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? referenceId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetRads` (`HSSDA.cs:791`) - `[HSS].[uspGetRads]`.</summary>
    Task<DataTable> GetRadsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetRadResults` (`HSSDA.cs:766`) - `[HSS].[uspGetRadResults]`.</summary>
    Task<DataTable> GetRadResultsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? referenceId, CancellationToken ct = default);

    /// <summary>
    /// `HSSDA.GetOtherDocs` (`HSSDA.cs:653-726`) - real branch is `CheckAWSIsEnabled(practiceSuffixNumeric,
    /// connectionString)` -&gt; `[HSS].[uspGetOtherDocs_AWS]` (+ per-row `DataType` enrichment via
    /// `GetDocumentStatusFromIndici`) vs plain `[HSS].[uspGetOtherDocs]`. `isReferral` true adds
    /// `@pType = "Discharge Summary"`.
    /// </summary>
    Task<DataTable> GetOtherDocsAsync(string practiceSuffix, string practiceSuffixNumeric, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, bool isReferral, CancellationToken ct = default);

    /// <summary>
    /// `HSSDA.GetDocResults` (`HSSDA.cs:260-345`) - real branch is `CheckAWSIsEnabled(practiceSuffixNumeric,
    /// connectionString)` -&gt; `[HSS].[uspGetDocResults_AWS]` (+ single-document `Content`/`DocumentId`/
    /// `DataType` enrichment via `DocumentGetByDocumentKeyJsonResult` when `referenceId` is present) vs
    /// plain `[HSS].[uspGetDocResults]`. `@pIsDischarge` always sent, `@pReferenceId` only when non-blank.
    /// </summary>
    Task<DataTable> GetDocResultsAsync(string practiceSuffix, string practiceSuffixNumeric, RoutingContext routingContext, string? referenceId, bool isDischarge, CancellationToken ct = default);
}
