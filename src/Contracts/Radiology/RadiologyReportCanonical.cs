using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Radiology;

/// <summary>
/// Unified radiology-report shape spanning HISO/ERMS only - KARO has no real radiology operation at
/// all (confirmed gap from the real compat controller, not an oversight). ERMS's real
/// `[HSS].[uspGetRads]` and HISO's real `Patient_RadiologyReport` concept resolve to the same
/// report/document-level shape as Documents/LabResults (SendingFacility/Subject/Name/DateReceived/
/// DataType/Comments) - no individual-result value exists for either source.
/// </summary>
public sealed record RadiologyReportCanonical(
    string? ReferenceId,
    string? Name,
    string? Subject,
    string? DataType,
    string? DateReceived,
    string? Comments,
    OriginScope Source);
