using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Reports;
using HekCoreApi.Contracts.Security;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Reports;

/// <summary>FLAGGED INFERENCES: procedure/column names follow the same conventions documented on other Block 2 repositories - none confirmed against a live schema.</summary>
public sealed class ReportsRepository : IReportsRepository
{
    private readonly IHisoConceptExecutor _hisoExecutor;
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public ReportsRepository(IHisoConceptExecutor hisoExecutor, ILegacyPracticeConnectionResolver connectionResolver)
    {
        _hisoExecutor = hisoExecutor;
        _connectionResolver = connectionResolver;
    }

    public async Task<IReadOnlyList<ReportSummary>> GetListAsync(
        ReportKind kind, OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession,
        DateOnly? sinceDate, DateOnly? untilDate, string? sortOrder, CancellationToken ct = default)
    {
        if (kind == ReportKind.Radiology && origin != OriginScope.Erms)
        {
            throw new Domain.Exceptions.ForbiddenException("Radiology results are only available for ERMS-origin tokens.");
        }

        if (origin == OriginScope.Hiso && kind == ReportKind.Lab)
        {
            const string procedureName = "Hiso.uspGetPatient_LaboratoryReport";
            var dataSet = await _hisoExecutor.ExecuteAsync(procedureName, hisoSession, [new HisoRequest { ProcedureName = procedureName }], ct);
            return MapSummaryRows(dataSet?.Tables.Count > 0 ? dataSet.Tables[0] : null, kind);
        }

        var procedure = (kind, origin) switch
        {
            (ReportKind.Lab, OriginScope.Karo) => "[HSS].[uspGetLabResults]",
            (ReportKind.Lab, OriginScope.Erms) => "[HSS].[uspGetLaboratoryReportList]",
            (ReportKind.Radiology, OriginScope.Erms) => "[HSS].[uspGetRadiologyReportList]",
            _ => throw new Domain.Exceptions.ForbiddenException($"{kind} results are not available for this origin scope.")
        };

        var connectionString = await _connectionResolver.ResolveAsync(hisoSession.PracticeId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pEncounterID", encounterId),
            new("@pSinceDate", (object?)sinceDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
            new("@pUntilDate", (object?)untilDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
            new("@pSortOrder", (object?)sortOrder ?? DBNull.Value)
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procedure, parameters, ct);
        return MapSummaryRows(table, kind);
    }

    public async Task<ReportContent?> GetDetailAsync(ReportKind kind, string practiceId, string reportId, CancellationToken ct = default)
    {
        var procedure = kind == ReportKind.Lab ? "[HSS].[uspGetLaboratoryReportDetails]" : "[HSS].[uspGetRadiologyReportDetails]";

        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pReportID", reportId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procedure, parameters, ct);

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        // ERMS-BR-13: legacy RTF+Base64 transcoding replaced with plain text per FR-LAB-01 - content
        // is returned as-is from the stored procedure, no RTF re-encoding performed.
        return new ReportContent(reportId, row["Content"]?.ToString() ?? string.Empty, "plain");
    }

    private static List<ReportSummary> MapSummaryRows(DataTable? table, ReportKind kind)
    {
        if (table is null)
        {
            return [];
        }

        var summaries = new List<ReportSummary>();
        foreach (DataRow row in table.Rows)
        {
            summaries.Add(new ReportSummary(
                row["ReportId"]?.ToString() ?? Guid.NewGuid().ToString(),
                row["Type"] is DBNull or null ? kind.ToString() : row["Type"].ToString()!,
                row["Date"] is DBNull or null ? DateTimeOffset.UtcNow : Convert.ToDateTime(row["Date"])));
        }

        return summaries;
    }
}
