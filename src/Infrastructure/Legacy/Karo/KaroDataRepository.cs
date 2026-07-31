using System.Data;
using System.Text.Json;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>Ported from `HSSDA.cs`'s remaining real GET-operation methods - one repository, matching legacy's single shared `HSSDA` static class.</summary>
public sealed class KaroDataRepository : IKaroDataRepository
{
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DOC"] = "application/msword",
        ["JPEG"] = "image/jpeg",
        ["HTML"] = "text/html",
        ["JPG"] = "image/jpeg",
        ["BMP"] = "image/bmp",
        ["PDF"] = "application/pdf",
        ["PNG"] = "image/png",
        ["GIF"] = "image/gif",
        ["TIF"] = "image/tiff",
        ["TIFF"] = "image/tiff",
        ["RTF"] = "application/rtf",
        ["TXT"] = "text/plain",
        ["DOCX"] = "application/msword",
        ["XML"] = "application/xml"
    };

    private readonly IKaroPracticeConnectionResolver _connectionResolver;
    private readonly IKaroDmsConnectionResolver _dmsConnectionResolver;
    private readonly IAwsDocumentService _awsDocumentService;

    public KaroDataRepository(IKaroPracticeConnectionResolver connectionResolver, IKaroDmsConnectionResolver dmsConnectionResolver, IAwsDocumentService awsDocumentService)
    {
        _connectionResolver = connectionResolver;
        _dmsConnectionResolver = dmsConnectionResolver;
        _awsDocumentService = awsDocumentService;
    }

    public async Task<List<KaroConsultNote>> GetConsultNotesAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default) =>
        await RunAsync<KaroConsultNote>(routingContext, "[HSS].[uspGetConsultNotes]", Param("@pPatientId", patientId), ct);

    public async Task<List<KaroDiagnosis>> GetConditionsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default) =>
        await RunAsync<KaroDiagnosis>(routingContext, "[HSS].[uspGetConditions]", Param("@pPatientId", patientId), ct);

    /// <summary>
    /// Legacy (`hsswebapi/DAL/South/HSSDA.cs:262-373`): `CheckAWSIsEnabled(practiceSuffixNumeric,
    /// connectionString)` picks the plain `[HSS].[uspGetDocuments]` path when disabled. When enabled,
    /// always runs `[HSS].[uspGetDocuments_AWS]` first, then either (a) no identifier: enriches every
    /// row's ContentType via `GetDocumentStatusFromIndici(identifier, practiceId, connectionString)`, or
    /// (b) an identifier given: fetches the single document's JSON via
    /// `DocumentGetByDocumentKeyJsonResult(identifier, practiceId, connectionString, dmsConnectionString)`
    /// and overwrites the first row's MessageData/MessageTitle/ContentType from it.
    /// </summary>
    public async Task<List<KaroDocumentInfo>> GetDocumentsAsync(string practiceSuffix, string practiceSuffixNumeric, RoutingContext routingContext, string? patientId, string? identifier, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        var parameters = new List<SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value) };
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            parameters.Add(new SqlParameter("@pIdentifier", identifier));
        }

        if (!int.TryParse(practiceSuffixNumeric, out var practiceIdInt))
        {
            var table0 = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocuments]", parameters, ct);
            return HekCoreApi.Infrastructure.Legacy.Hiso.DataTableMapper.ToList<KaroDocumentInfo>(table0);
        }

        var awsEnabled = await _awsDocumentService.CheckAwsIsEnabledAsync(practiceIdInt, connectionString, ct);
        if (!awsEnabled)
        {
            var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocuments]", parameters, ct);
            return HekCoreApi.Infrastructure.Legacy.Hiso.DataTableMapper.ToList<KaroDocumentInfo>(table);
        }

        var awsTable = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocuments_AWS]", parameters, ct);
        var results = HekCoreApi.Infrastructure.Legacy.Hiso.DataTableMapper.ToList<KaroDocumentInfo>(awsTable);

        if (string.IsNullOrEmpty(identifier))
        {
            foreach (var doc in results)
            {
                if (string.IsNullOrWhiteSpace(doc.Identifier))
                {
                    continue;
                }

                var status = await _awsDocumentService.GetDocumentStatusFromIndiciAsync(doc.Identifier.ToUpperInvariant(), practiceIdInt, connectionString, ct);
                if (status?.DocumentType is { } docType)
                {
                    doc.ContentType = MimeTypes.GetValueOrDefault(docType);
                }
            }

            return results;
        }

        var dmsConnectionString = await _dmsConnectionResolver.ResolveAsync(routingContext, ct);
        var singleDocJson = await _awsDocumentService.DocumentGetByDocumentKeyJsonResultAsync(identifier.ToUpperInvariant(), practiceIdInt, dmsConnectionString, connectionString, ct);
        if (string.IsNullOrEmpty(singleDocJson) || results.Count == 0)
        {
            return results;
        }

        var singleDoc = JsonSerializer.Deserialize<KaroAwsSingleDocument>(singleDocJson, JsonOptions);
        if (singleDoc is null)
        {
            return results;
        }

        results[0].MessageData = singleDoc.DocumentData;
        results[0].MessageTitle = singleDoc.DocumentName;
        results[0].ContentType = singleDoc.DocumentType is { } t ? MimeTypes.GetValueOrDefault(t) : null;
        return results;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record KaroAwsSingleDocument(byte[]? DocumentData, string? DocumentName, string? DocumentType);

    public async Task<List<KaroLabResult>> GetLabResultsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default) =>
        await RunAsync<KaroLabResult>(routingContext, "[HSS].[uspGetLabResults]", Param("@pPatientId", patientId), ct);

    public async Task<List<KaroMedication>> GetMedicationsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value), new("@pShowStop", false) };
        return await RunAsync<KaroMedication>(routingContext, "[HSS].[uspGetMedications]", parameters, ct);
    }

    public async Task<List<KaroObservation>> GetObservationsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? conceptId, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value) };
        if (!string.IsNullOrWhiteSpace(conceptId))
        {
            parameters.Add(new SqlParameter("@pScreeningCode", conceptId));
        }

        return await RunAsync<KaroObservation>(routingContext, "[HSS].[uspGetObservations]", parameters, ct);
    }

    public async Task<List<KaroProvider>> GetProviderAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? userId, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value) };
        if (!string.IsNullOrWhiteSpace(userId))
        {
            parameters.Add(new SqlParameter("@pUserId", userId));
        }

        return await RunAsync<KaroProvider>(routingContext, "[HSS].[uspGetProvider]", parameters, ct);
    }

    /// <summary>Legacy: `practiceid2` (`@pPracticeid`) is a real dead variable in `APIController.cs` - declared but never assigned, always the empty string. Reproduced exactly.</summary>
    public async Task<List<KaroRecallCategory>> GetRecallCategoriesAsync(string practiceSuffix, RoutingContext routingContext, string? group, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pRecallGroup", (object?)group ?? DBNull.Value), new("@pPracticeid", string.Empty) };
        return await RunAsync<KaroRecallCategory>(routingContext, "[HSS].[uspGetRecallCategories]", parameters, ct);
    }

    public async Task<List<KaroRecall>> GetRecallsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default) =>
        await RunAsync<KaroRecall>(routingContext, "[HSS].[uspGetRecalls]", Param("@pPatientId", patientId), ct);

    public async Task<List<KaroScreeningCode>> GetScreeningCodesAsync(string practiceSuffix, RoutingContext routingContext, CancellationToken ct = default) =>
        await RunAsync<KaroScreeningCode>(routingContext, "[HSS].[uspGetScreeningCodes]", Param("@pPracticeId", "6"), ct);

    public async Task<List<KaroPatientAttachment>> GetPatientAttachmentAsync(string practiceSuffix, string practiceSuffixNumeric, RoutingContext routingContext, string? patientId, string? referenceId, string? sortOrder, string? subject, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrEmpty(patientId))
        {
            parameters.Add(new SqlParameter("@pPatientId", int.Parse(patientId)));
        }

        parameters.Add(new SqlParameter("@pPracticeID", practiceSuffixNumeric));
        if (dateFrom is { } from)
        {
            parameters.Add(new SqlParameter("@pFromDate", from));
        }

        if (dateTo is { } to)
        {
            parameters.Add(new SqlParameter("@pToDate", to));
        }

        if (!string.IsNullOrEmpty(subject))
        {
            parameters.Add(new SqlParameter("@pSearch", int.Parse(subject)));
        }

        if (!string.IsNullOrWhiteSpace(sortOrder))
        {
            parameters.Add(new SqlParameter("@pSortBy", sortOrder));
        }

        if (!string.IsNullOrEmpty(referenceId))
        {
            parameters.Add(new SqlParameter("@pReferenceID", referenceId));
        }

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetPatientDMS]", parameters, ct);
        var results = new List<KaroPatientAttachment>();
        foreach (DataRow row in table.Rows)
        {
            results.Add(new KaroPatientAttachment
            {
                AttachmentReferenceID = Str(row, "AttachmentReferenceID"),
                AttachmentSubject = Str(row, "AttachmentSubject"),
                AttachmentComments = Str(row, "AttachmentComments"),
                AttachmentType = Str(row, "AttachmentType"),
                AttachmentDataType = Str(row, "AttachmentDataType"),
                AttachmentCreationDate = Str(row, "AttachmentCreationDate"),
                AttachmentContent = row.Table.Columns.Contains("AttachmentContent") && row["AttachmentContent"] is byte[] bytes ? Convert.ToBase64String(bytes) : null,
                AttachmentSize = Str(row, "AttachmentSize")
            });
        }

        return results;
    }

    // Returns "" (not null) for a DBNull column, matching real legacy's `Convert.ChangeType(DBNull.Value,
    // typeof(string))` behavior - see DataTableMapper.cs's doc comment for the confirmed root cause.
    private static string Str(DataRow row, string column) =>
        row.Table.Columns.Contains(column) && row[column] is not DBNull ? row[column].ToString() ?? string.Empty : string.Empty;

    private async Task<List<T>> RunAsync<T>(RoutingContext routingContext, string procedureName, List<SqlParameter> parameters, CancellationToken ct) where T : new()
    {
        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procedureName, parameters, ct);
        return HekCoreApi.Infrastructure.Legacy.Hiso.DataTableMapper.ToList<T>(table);
    }

    private static List<SqlParameter> Param(string name, string? value) => new() { new SqlParameter(name, (object?)value ?? DBNull.Value) };
}
