using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Documents;
using HekCoreApi.Contracts.Security;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Documents;

/// <summary>
/// FLAGGED INFERENCES: procedure/column names follow the same conventions documented on other
/// Block 2 repositories - none confirmed against a live schema.
///
/// FLAGGED GAP, NOT IMPLEMENTED: HISO's document save capability (`saveContainer`) includes
/// Aspose-based HTML/image-to-PDF/DOCX rendering, which the stakeholder decided to "keep,
/// implemented as the old system did it" (PROJECT_STATUS.md). Aspose is commercially licensed
/// software - no license or package is available in this environment to actually port that
/// rendering step. This repository stores document content as supplied (no format conversion);
/// the Aspose rendering step itself is not implemented and must be added once licensing is
/// available - tracked as an open item, not silently dropped or faked with a different library
/// (which would violate the "no replacement" stakeholder decision).
/// </summary>
public sealed class DocumentsRepository : IDocumentsRepository
{
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public DocumentsRepository(ILegacyPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IReadOnlyList<DocumentSummary>> GetListAsync(
        OriginScope origin, int patientId, string practiceId, string? direction, string? contentType,
        string? referenceId, string? subject, DateOnly? sinceDate, DateOnly? untilDate, string? sortOrder, CancellationToken ct = default)
    {
        var procedure = origin switch
        {
            OriginScope.Karo => "[HSS].[uspGetDocuments]",
            OriginScope.Erms => "[HSS].[uspGetScannedList]",
            OriginScope.Hiso => "[HSS].[uspGetDocuments]",
            _ => throw new Domain.Exceptions.ForbiddenException("Documents are not available for this origin scope.")
        };

        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pDirection", (object?)direction ?? DBNull.Value),
            new("@pContentType", (object?)contentType ?? DBNull.Value),
            new("@pReferenceID", (object?)referenceId ?? DBNull.Value),
            new("@pSubject", (object?)subject ?? DBNull.Value),
            new("@pSinceDate", (object?)sinceDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
            new("@pUntilDate", (object?)untilDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
            new("@pSortOrder", (object?)sortOrder ?? DBNull.Value)
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procedure, parameters, ct);
        return MapSummaryRows(table);
    }

    public async Task<Document?> GetDetailAsync(string practiceId, string documentId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pDocumentID", documentId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetScannedDetails]", parameters, ct);

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        var summary = MapSummaryRow(row);
        var content = row["Content"] is DBNull or null ? string.Empty : Convert.ToBase64String((byte[])row["Content"]);
        return Document.FromSummary(summary, content);
    }

    public async Task<DocumentSummary?> FindByReferenceIdAsync(string practiceId, string referenceId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(referenceId))
        {
            return null;
        }

        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pReferenceID", referenceId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspFindDocumentByReferenceId]", parameters, ct);
        return table.Rows.Count > 0 ? MapSummaryRow(table.Rows[0]) : null;
    }

    public async Task<DocumentSummary> SaveAsync(int patientId, string practiceId, DocumentInput input, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pDirection", input.Direction),
            new("@pContentType", input.ContentType),
            new("@pSubject", (object?)input.Subject ?? DBNull.Value),
            new("@pReferenceID", (object?)input.ReferenceId ?? DBNull.Value),
            new("@pContent", SqlDbType.VarBinary) { Value = Convert.FromBase64String(input.Content) }
        };

        var output = new SqlParameter("@pDocumentIDOut", SqlDbType.NVarChar, 64) { Direction = ParameterDirection.Output };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspSaveDocument]", parameters, ct);

        return new DocumentSummary(output.Value?.ToString() ?? Guid.NewGuid().ToString(), patientId, input.Direction, input.ContentType, DateTimeOffset.UtcNow, input.Subject, input.ReferenceId);
    }

    private static List<DocumentSummary> MapSummaryRows(DataTable table)
    {
        var summaries = new List<DocumentSummary>();
        foreach (DataRow row in table.Rows)
        {
            summaries.Add(MapSummaryRow(row));
        }

        return summaries;
    }

    private static DocumentSummary MapSummaryRow(DataRow row) => new(
        row["DocumentId"]?.ToString() ?? Guid.NewGuid().ToString(),
        row["PatientId"] is DBNull or null ? 0 : Convert.ToInt32(row["PatientId"]),
        row["Direction"]?.ToString() ?? "in",
        row["ContentType"]?.ToString() ?? string.Empty,
        row["CreatedAt"] is DBNull or null ? DateTimeOffset.UtcNow : Convert.ToDateTime(row["CreatedAt"]),
        row["Subject"] is DBNull or null ? null : row["Subject"].ToString(),
        row["ReferenceId"] is DBNull or null ? null : row["ReferenceId"].ToString());
}
