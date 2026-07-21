using System.Data;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from ERMS's `HSSDA` read calls (`DAL/South/HSSDA.cs`) used by the Get* operations -
/// consolidated into one repository (same precedent as KARO's `IKaroDataRepository`, matching
/// legacy's own single shared `HSSDA` static class). Each method returns the raw pipe-delimited
/// DataTable; `ErmsDataTableMapper` in the adapter layer does the HISO mapping.
/// Date parameters follow legacy exactly: `DateTime.MinValue` means "omit the SQL parameter".
/// </summary>
public interface IErmsDataRepository
{
    /// <summary>`HSSDA.GetMeasurement` (`HSSDA.cs:520`) - `[HSS].[uspGetMeasurement]`. Legacy passes an `encounterId` arg it never uses - omitted here.</summary>
    Task<DataTable> GetMeasurementAsync(string practiceSuffix, string? patientId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetSmokingStatus` (`HSSDA.cs:907`) - `[HSS].[uspGetSmokingStatus]`.</summary>
    Task<DataTable> GetSmokingStatusAsync(string practiceSuffix, string? patientId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetProvider` (`HSSDA.cs:738`) - `[HSS].[uspGetProvider]`; userId/locationId/encounterId params only sent when non-blank.</summary>
    Task<DataTable> GetProviderAsync(string practiceSuffix, string? patientId, string? userId, string? locationId, string? encounterId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetNextOfKin` (`HSSDA.cs:608`) - `[HSS].[uspGetNextOfKin]`.</summary>
    Task<DataTable> GetNextOfKinAsync(string practiceSuffix, string? patientId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetRegisteredPractitioners` (`HSSDA.cs:862`) - `[HSS].[uspGetRegisteredPractitioners]`; locationId only sent when non-blank.</summary>
    Task<DataTable> GetRegisteredPractitionersAsync(string practiceSuffix, string? patientId, string? locationId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetACC45` (`HSSDA.cs:143`) - `[HSS].[uspGetACC45]`.</summary>
    Task<DataTable> GetAcc45Async(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetConditions` (`HSSDA.cs:177`) - `[HSS].[uspGetConditions]` (legacy `GetClassifications` operation).</summary>
    Task<DataTable> GetConditionsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetConsultNotes` (`HSSDA.cs:210`) - `[HSS].[uspGetConsultNotes]`.</summary>
    Task<DataTable> GetConsultNotesAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetMedicalAllergies` (`HSSDA.cs:541`) - real proc is `[HSS].[uspGetAllergies]`.</summary>
    Task<DataTable> GetMedicalAllergiesAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetMedications` (`HSSDA.cs:575`) - `[HSS].[uspGetMedications]`; `@pIsLongTerm` false=Prescribed / true=Regular, `@pShowStop` always false.</summary>
    Task<DataTable> GetMedicationsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, bool isLongTerm, CancellationToken ct = default);

    /// <summary>`HSSDA.GetLabs` (`HSSDA.cs:491`) - `[HSS].[uspGetLabs]`.</summary>
    Task<DataTable> GetLabsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetLabResults` (`HSSDA.cs:461`) - `[HSS].[uspGetLabResults]`; both params only sent when non-blank.</summary>
    Task<DataTable> GetLabResultsAsync(string practiceSuffix, string? patientId, string? referenceId, CancellationToken ct = default);

    /// <summary>`HSSDA.GetRads` (`HSSDA.cs:791`) - `[HSS].[uspGetRads]`.</summary>
    Task<DataTable> GetRadsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default);

    /// <summary>`HSSDA.GetRadResults` (`HSSDA.cs:766`) - `[HSS].[uspGetRadResults]`.</summary>
    Task<DataTable> GetRadResultsAsync(string practiceSuffix, string? patientId, string? referenceId, CancellationToken ct = default);

    /// <summary>
    /// `HSSDA.GetOtherDocs` (`HSSDA.cs:653`) - non-AWS path only (`[HSS].[uspGetOtherDocs]`); the AWS branch
    /// needs the non-portable `AWSDoc.IndiciDMS` DLL - same documented deferral as KARO/HISO's AWS gaps.
    /// `isReferral` true adds `@pType = "Discharge Summary"`.
    /// </summary>
    Task<DataTable> GetOtherDocsAsync(string practiceSuffix, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, bool isReferral, CancellationToken ct = default);

    /// <summary>`HSSDA.GetDocResults` (`HSSDA.cs:260`) - non-AWS path only (`[HSS].[uspGetDocResults]`); `@pIsDischarge` always sent, `@pReferenceId` only when non-blank (legacy's referralId arg is always blank from the API).</summary>
    Task<DataTable> GetDocResultsAsync(string practiceSuffix, string? referenceId, bool isDischarge, CancellationToken ct = default);
}
