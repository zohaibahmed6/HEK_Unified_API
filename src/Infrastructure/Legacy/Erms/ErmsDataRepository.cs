using System.Data;
using System.Text.Json;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>
/// Ported from ERMS's `HSSDA` read calls (`DAL/South/HSSDA.cs`) - real procs on
/// `ConnIndiciDB{practiceid}`. Sparse-parameter rules reproduced exactly (sort/date/optional
/// params only added when non-blank / not MinValue).
/// </summary>
public sealed class ErmsDataRepository : IErmsDataRepository
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

    private readonly IErmsPracticeConnectionResolver _connectionResolver;
    private readonly IErmsDmsConnectionResolver _dmsConnectionResolver;
    private readonly IAwsDocumentService _awsDocumentService;

    public ErmsDataRepository(IErmsPracticeConnectionResolver connectionResolver, IErmsDmsConnectionResolver dmsConnectionResolver, IAwsDocumentService awsDocumentService)
    {
        _connectionResolver = connectionResolver;
        _dmsConnectionResolver = dmsConnectionResolver;
        _awsDocumentService = awsDocumentService;
    }

    public Task<DataTable> GetMeasurementAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetMeasurement]", PatientOnly(patientId), ct);

    public Task<DataTable> GetSmokingStatusAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetSmokingStatus]", PatientOnly(patientId), ct);

    public Task<DataTable> GetProviderAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? userId, string? locationId, string? encounterId, CancellationToken ct = default)
    {
        var parameters = PatientOnly(patientId);
        if (!string.IsNullOrWhiteSpace(locationId))
        {
            parameters.Add(new SqlParameter("@pLocationId", locationId));
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            parameters.Add(new SqlParameter("@pUserId", userId));
        }

        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            parameters.Add(new SqlParameter("@pEncounterid", encounterId));
        }

        return ExecuteAsync(routingContext, "[HSS].[uspGetProvider]", parameters, ct);
    }

    public Task<DataTable> GetNextOfKinAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetNextOfKin]", PatientOnly(patientId), ct);

    public async Task<DataTable> GetRegisteredPractitionersAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? locationId, CancellationToken ct = default)
    {
        var parameters = PatientOnly(patientId);
        if (!string.IsNullOrWhiteSpace(locationId))
        {
            parameters.Add(new SqlParameter("@pLocationId", locationId));
        }

        var table = await ExecuteAsync(routingContext, "[HSS].[uspGetRegisteredPractitioners]", parameters, ct);
        return StableSort(table, dateColumn: null, sortOrder: null);
    }

    public Task<DataTable> GetAcc45Async(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetACC45]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public Task<DataTable> GetConditionsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetConditions]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public async Task<DataTable> GetConsultNotesAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default)
    {
        var table = await ExecuteAsync(routingContext, "[HSS].[uspGetConsultNotes]", Dated(patientId, sortOrder, minDate, maxDate), ct);
        return StableSort(table, dateColumn: "date", sortOrder);
    }

    public Task<DataTable> GetMedicalAllergiesAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetAllergies]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public async Task<DataTable> GetMedicationsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, bool isLongTerm, CancellationToken ct = default)
    {
        var parameters = Dated(patientId, sortOrder, minDate, maxDate);
        parameters.Add(new SqlParameter("@pIsLongTerm", isLongTerm));
        parameters.Add(new SqlParameter("@pShowStop", false));
        var table = await ExecuteAsync(routingContext, "[HSS].[uspGetMedications]", parameters, ct);
        return StableSort(table, dateColumn: "startDate", sortOrder);
    }

    public Task<DataTable> GetLabsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetLabs]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public Task<DataTable> GetLabResultsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? referenceId, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetLabResults]", Detail(patientId, referenceId), ct);

    public Task<DataTable> GetRadsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetRads]", Dated(patientId, sortOrder, minDate, maxDate), ct);

    public Task<DataTable> GetRadResultsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? referenceId, CancellationToken ct = default) =>
        ExecuteAsync(routingContext, "[HSS].[uspGetRadResults]", Detail(patientId, referenceId), ct);

    public async Task<DataTable> GetOtherDocsAsync(string practiceSuffix, string practiceSuffixNumeric, RoutingContext routingContext, string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate, bool isReferral, CancellationToken ct = default)
    {
        var parameters = Dated(patientId, sortOrder, minDate, maxDate);
        if (isReferral)
        {
            parameters.Add(new SqlParameter("@pType", "Discharge Summary"));
        }

        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        if (!int.TryParse(practiceSuffixNumeric, out var practiceIdInt))
        {
            return await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetOtherDocs]", parameters, ct);
        }

        var awsEnabled = await _awsDocumentService.CheckAwsIsEnabledAsync(practiceIdInt, connectionString, ct);
        if (!awsEnabled)
        {
            return await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetOtherDocs]", parameters, ct);
        }

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetOtherDocs_AWS]", parameters, ct);
        foreach (DataRow row in table.Rows)
        {
            if (!table.Columns.Contains("DMSID") || row["DMSID"] is DBNull)
            {
                continue;
            }

            var docKey = row["DMSID"].ToString()?.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(docKey))
            {
                continue;
            }

            var status = await _awsDocumentService.GetDocumentStatusFromIndiciAsync(docKey, practiceIdInt, connectionString, ct);
            if (status?.DocumentType is { } docType && MimeTypes.TryGetValue(docType, out var contentType) && table.Columns.Contains("DataType"))
            {
                // `uspGetOtherDocs_AWS`'s DataType column comes back flagged read-only by SQL Server's
                // schema metadata (a computed/derived column in that proc's result set) - DataTable.Load
                // copies that flag over, so a direct write throws "Column 'DataType' is read only." even
                // though the plain (non-AWS) uspGetOtherDocs doesn't have this issue. Clear it once per
                // table before enriching, same real-data enrichment legacy performs unconditionally.
                table.Columns["DataType"]!.ReadOnly = false;
                row["DataType"] = row["DataType"] is DBNull ? contentType : row["DataType"] + contentType;
            }
        }

        return table;
    }

    public async Task<DataTable> GetDocResultsAsync(string practiceSuffix, string practiceSuffixNumeric, RoutingContext routingContext, string? referenceId, bool isDischarge, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pIsDischarge", isDischarge) };
        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            parameters.Add(new SqlParameter("@pReferenceId", referenceId));
        }

        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        if (!int.TryParse(practiceSuffixNumeric, out var practiceIdInt))
        {
            return await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocResults]", parameters, ct);
        }

        var awsEnabled = await _awsDocumentService.CheckAwsIsEnabledAsync(practiceIdInt, connectionString, ct);
        if (!awsEnabled)
        {
            return await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocResults]", parameters, ct);
        }

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocResults_AWS]", parameters, ct);
        if (string.IsNullOrWhiteSpace(referenceId) || table.Rows.Count == 0)
        {
            return table;
        }

        var dmsConnectionString = await _dmsConnectionResolver.ResolveAsync(routingContext, ct);
        var singleDocJson = await _awsDocumentService.DocumentGetByDocumentKeyJsonResultAsync(referenceId.ToUpperInvariant(), practiceIdInt, dmsConnectionString, connectionString, ct);
        if (string.IsNullOrEmpty(singleDocJson))
        {
            return table;
        }

        var singleDoc = JsonSerializer.Deserialize<ErmsAwsSingleDocument>(singleDocJson, JsonOptions);
        if (singleDoc is null)
        {
            return table;
        }

        var firstRow = table.Rows[0];
        var base64 = singleDoc.DocumentData is null ? string.Empty : Convert.ToBase64String(singleDoc.DocumentData);
        // `uspGetDocResults_AWS`'s result columns can come back flagged read-only by SQL Server's schema
        // metadata (computed/derived columns) - DataTable.Load copies that flag over, so a direct write
        // throws "Column 'X' is read only." Clear it before writing, same enrichment legacy performs
        // unconditionally on plain (non-computed-flagged) columns from the equivalent non-AWS proc.
        if (table.Columns.Contains("Content"))
        {
            table.Columns["Content"]!.ReadOnly = false;
            table.Columns["Content"]!.MaxLength = -1;
            firstRow["Content"] = (firstRow["Content"] is DBNull ? string.Empty : firstRow["Content"]) + base64;
        }

        if (table.Columns.Contains("DocumentId"))
        {
            table.Columns["DocumentId"]!.ReadOnly = false;
            table.Columns["DocumentId"]!.MaxLength = -1;
            firstRow["DocumentId"] = singleDoc.DocumentId;
        }

        if (table.Columns.Contains("DataType") && singleDoc.DocumentType is { } docType && MimeTypes.TryGetValue(docType, out var contentType))
        {
            table.Columns["DataType"]!.ReadOnly = false;
            table.Columns["DataType"]!.MaxLength = -1;
            firstRow["DataType"] = (firstRow["DataType"] is DBNull ? string.Empty : firstRow["DataType"]) + contentType;
        }

        return table;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record ErmsAwsSingleDocument(int DocumentId, byte[]? DocumentData, string? DocumentType);

    /// <summary>Legacy `GetLabResults`/`GetRadResults`: both params only added when non-blank.</summary>
    private static List<SqlParameter> Detail(string? patientId, string? referenceId)
    {
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(patientId))
        {
            parameters.Add(new SqlParameter("@pPatientId", patientId));
        }

        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            parameters.Add(new SqlParameter("@pReferenceId", referenceId));
        }

        return parameters;
    }

    private static List<SqlParameter> PatientOnly(string? patientId) =>
        new() { new SqlParameter("@pPatientId", (object?)patientId ?? DBNull.Value) };

    private static List<SqlParameter> Dated(string? patientId, string? sortOrder, DateTime minDate, DateTime maxDate)
    {
        var parameters = PatientOnly(patientId);
        if (!string.IsNullOrWhiteSpace(sortOrder))
        {
            parameters.Add(new SqlParameter("@pSortOrder", sortOrder));
        }

        if (!minDate.Equals(DateTime.MinValue))
        {
            parameters.Add(new SqlParameter("@pMinDate", minDate));
        }

        if (!maxDate.Equals(DateTime.MinValue))
        {
            parameters.Add(new SqlParameter("@pMaxDate", maxDate));
        }

        return parameters;
    }

    private async Task<DataTable> ExecuteAsync(RoutingContext routingContext, string procName, List<SqlParameter> parameters, CancellationToken ct)
    {
        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        return await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procName, parameters, ct);
    }

    /// <summary>
    /// The underlying SPs have no stable ORDER BY, so SQL Server can return rows in a different
    /// physical order on every call - confirmed live against real legacy itself (same request, two
    /// calls, two different row orders; the row *set* always matched). Rather than leave that
    /// non-determinism in our own output too, impose a deterministic sort here: by <paramref
    /// name="dateColumn"/> (matching <paramref name="sortOrder"/>, "ASC"/"DESC") when present, then
    /// always by <c>ReferenceId</c> as a tiebreaker so ties on the same date - or tables with no date
    /// column at all - still come back in the same order every time.
    /// </summary>
    private static DataTable StableSort(DataTable table, string? dateColumn, string? sortOrder)
    {
        var clauses = new List<string>();
        if (dateColumn is not null && table.Columns.Contains(dateColumn))
        {
            clauses.Add($"[{dateColumn}] {(string.Equals(sortOrder, "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}");
        }

        if (table.Columns.Contains("ReferenceId"))
        {
            clauses.Add("[ReferenceId] ASC");
        }

        if (clauses.Count == 0)
        {
            return table;
        }

        table.DefaultView.Sort = string.Join(", ", clauses);
        return table.DefaultView.ToTable();
    }
}
