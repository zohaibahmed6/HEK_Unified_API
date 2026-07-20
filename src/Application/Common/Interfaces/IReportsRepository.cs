using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Reports;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Application.Common.Interfaces;

public enum ReportKind
{
    Lab,
    Radiology
}

/// <summary>
/// Lab: HISO getData / KARO GetLabResults / ERMS GetLaboratoryReportList(+Details). Radiology:
/// ERMS GetRadiologyReportList(+Details) only - no HISO/KARO source documented for radiology
/// (Contract Design doc Section 4.6).
/// </summary>
public interface IReportsRepository
{
    Task<IReadOnlyList<ReportSummary>> GetListAsync(
        ReportKind kind, OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession,
        DateOnly? sinceDate, DateOnly? untilDate, string? sortOrder, CancellationToken ct = default);

    Task<ReportContent?> GetDetailAsync(ReportKind kind, string practiceId, string reportId, CancellationToken ct = default);
}
