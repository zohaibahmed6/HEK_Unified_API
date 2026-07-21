using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>Ported from `HSSDA.cs`'s remaining real GET-operation methods - one repository, matching legacy's single shared `HSSDA` static class.</summary>
public sealed class KaroDataRepository : IKaroDataRepository
{
    private readonly IKaroPracticeConnectionResolver _connectionResolver;

    public KaroDataRepository(IKaroPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<List<KaroConsultNote>> GetConsultNotesAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        await RunAsync<KaroConsultNote>(practiceSuffix, "[HSS].[uspGetConsultNotes]", Param("@pPatientId", patientId), ct);

    public async Task<List<KaroDiagnosis>> GetConditionsAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        await RunAsync<KaroDiagnosis>(practiceSuffix, "[HSS].[uspGetConditions]", Param("@pPatientId", patientId), ct);

    /// <summary>Legacy: real branch is `AWSDoc.IndiciDMS.CheckAWSIsEnabled(...)` -> `[HSS].[uspGetDocuments_AWS]` vs `[HSS].[uspGetDocuments]`. AWSDoc's real source isn't portable (compiled DLL only, same gap as HISO's AWS deferral) - always takes the non-AWS real path, flagged not guessed.</summary>
    public async Task<List<KaroDocumentInfo>> GetDocumentsAsync(string practiceSuffix, string? patientId, string? identifier, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value) };
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            parameters.Add(new SqlParameter("@pIdentifier", identifier));
        }

        return await RunAsync<KaroDocumentInfo>(practiceSuffix, "[HSS].[uspGetDocuments]", parameters, ct);
    }

    public async Task<List<KaroLabResult>> GetLabResultsAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        await RunAsync<KaroLabResult>(practiceSuffix, "[HSS].[uspGetLabResults]", Param("@pPatientId", patientId), ct);

    public async Task<List<KaroMedication>> GetMedicationsAsync(string practiceSuffix, string? patientId, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value), new("@pShowStop", false) };
        return await RunAsync<KaroMedication>(practiceSuffix, "[HSS].[uspGetMedications]", parameters, ct);
    }

    public async Task<List<KaroObservation>> GetObservationsAsync(string practiceSuffix, string? patientId, string? conceptId, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value) };
        if (!string.IsNullOrWhiteSpace(conceptId))
        {
            parameters.Add(new SqlParameter("@pScreeningCode", conceptId));
        }

        return await RunAsync<KaroObservation>(practiceSuffix, "[HSS].[uspGetObservations]", parameters, ct);
    }

    public async Task<List<KaroProvider>> GetProviderAsync(string practiceSuffix, string? patientId, string? userId, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value) };
        if (!string.IsNullOrWhiteSpace(userId))
        {
            parameters.Add(new SqlParameter("@pUserId", userId));
        }

        return await RunAsync<KaroProvider>(practiceSuffix, "[HSS].[uspGetProvider]", parameters, ct);
    }

    /// <summary>Legacy: `practiceid2` (`@pPracticeid`) is a real dead variable in `APIController.cs` - declared but never assigned, always the empty string. Reproduced exactly.</summary>
    public async Task<List<KaroRecallCategory>> GetRecallCategoriesAsync(string practiceSuffix, string? group, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter> { new("@pRecallGroup", (object?)group ?? DBNull.Value), new("@pPracticeid", string.Empty) };
        return await RunAsync<KaroRecallCategory>(practiceSuffix, "[HSS].[uspGetRecallCategories]", parameters, ct);
    }

    public async Task<List<KaroRecall>> GetRecallsAsync(string practiceSuffix, string? patientId, CancellationToken ct = default) =>
        await RunAsync<KaroRecall>(practiceSuffix, "[HSS].[uspGetRecalls]", Param("@pPatientId", patientId), ct);

    public async Task<List<KaroScreeningCode>> GetScreeningCodesAsync(string practiceSuffix, CancellationToken ct = default) =>
        await RunAsync<KaroScreeningCode>(practiceSuffix, "[HSS].[uspGetScreeningCodes]", Param("@pPracticeId", "6"), ct);

    public async Task<List<KaroPatientAttachment>> GetPatientAttachmentAsync(string practiceSuffix, string practiceSuffixNumeric, string? patientId, string? referenceId, string? sortOrder, string? subject, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceSuffix, ct);
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

    private static string? Str(DataRow row, string column) =>
        row.Table.Columns.Contains(column) && row[column] is not DBNull ? row[column].ToString() : null;

    private async Task<List<T>> RunAsync<T>(string practiceSuffix, string procedureName, List<SqlParameter> parameters, CancellationToken ct) where T : new()
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceSuffix, ct);
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procedureName, parameters, ct);
        return HekCoreApi.Infrastructure.Legacy.Hiso.DataTableMapper.ToList<T>(table);
    }

    private static List<SqlParameter> Param(string name, string? value) => new() { new SqlParameter(name, (object?)value ?? DBNull.Value) };
}
