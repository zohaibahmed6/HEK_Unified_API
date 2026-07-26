using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>Ported from ERMS `SaveDocument`'s DAL calls (`DAL/South/HSSDA.cs`) - real procs including the real AWS-vs-non-AWS branch (`AwsDocumentService`, confirmed against the real `AWSDocCore.dll`, same one HISO already uses).</summary>
public sealed class ErmsWriteRepository : IErmsWriteRepository
{
    private readonly IErmsPracticeConnectionResolver _connectionResolver;
    private readonly IErmsDmsConnectionResolver _dmsConnectionResolver;
    private readonly ISecretProvider _secretProvider;
    private readonly IAwsDocumentService _awsDocumentService;

    public ErmsWriteRepository(IErmsPracticeConnectionResolver connectionResolver, IErmsDmsConnectionResolver dmsConnectionResolver, ISecretProvider secretProvider, IAwsDocumentService awsDocumentService)
    {
        _connectionResolver = connectionResolver;
        _dmsConnectionResolver = dmsConnectionResolver;
        _secretProvider = secretProvider;
        _awsDocumentService = awsDocumentService;
    }

    /// <summary>Legacy (`HSSDA.cs:38-64`): `CheckAWSIsEnabled(practiceSuffixNumeric, connectionString)` picks `[HSS].[uspUpdateExistingDoc_AWS]` vs `[HSS].[uspUpdateExistingDoc]` - both take the same two params.</summary>
    public async Task UpdateExistingDocumentAsync(string practiceSuffix, string? referralId, string? practiceSuffixNumeric, CancellationToken ct = default)
    {
        // Legacy swallows every error here (its own try/catch fills an out param the controller only logs).
        try
        {
            var connectionString = await _connectionResolver.ResolveAsync(practiceSuffix, ct);
            var parameters = new List<SqlParameter>
            {
                new("@pReferralId", (object?)referralId ?? DBNull.Value),
                new("@pPracticeId", (object?)practiceSuffixNumeric ?? DBNull.Value)
            };

            var procedureName = "[HSS].[uspUpdateExistingDoc]";
            if (int.TryParse(practiceSuffixNumeric, out var practiceIdInt))
            {
                var awsEnabled = await _awsDocumentService.CheckAwsIsEnabledAsync(practiceIdInt, connectionString, ct);
                if (awsEnabled)
                {
                    procedureName = "[HSS].[uspUpdateExistingDoc_AWS]";
                }
            }

            await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, procedureName, parameters, ct);
        }
        catch
        {
            // Matches legacy: failure only affects the log, never the response.
        }
    }

    public async Task<string> SaveToDmsAsync(string practiceSuffix, string? practiceSuffixNumeric, byte[] contentData, string? contentType, string? messageSubject, int categoryId, string? referralId, CancellationToken ct = default)
    {
        var documentTypeId = await ResolveDocumentTypeAsync(contentType, ct);
        var documentKey = Guid.NewGuid().ToString();
        var dmsConnectionString = await _dmsConnectionResolver.ResolveAsync(practiceSuffix, ct);

        var parameters = new List<SqlParameter>
        {
            new("@pClientID", 3),
            new("@pCategoryID", categoryId),
            new("@pDocumentName", (object?)messageSubject ?? DBNull.Value),
            new("@pDocumentTypeID", documentTypeId),
            new("@pDescription", (object?)referralId ?? DBNull.Value),
            new("@pDocumentKey", documentKey),
            new("@pDocumentSize", contentData.Length),
            new("@pProfileID", "1"),
            new("@pPracticeID", (object?)practiceSuffixNumeric ?? DBNull.Value),
            new("@pDocumentData", SqlDbType.VarBinary) { Value = contentData }
        };
        var outParam = new SqlParameter("@pDocumentIDOut", SqlDbType.Int) { Direction = ParameterDirection.Output, Value = -1 };
        parameters.Add(outParam);

        await LegacyDbExecutor.ExecuteNonQueryAsync(dmsConnectionString, CommandType.StoredProcedure, "[dbo].[uspDocumentSave]", parameters, ct);
        var dmsId = outParam.Value is DBNull or null ? -1 : Convert.ToInt64(outParam.Value);

        return dmsId > 0 ? documentKey : string.Empty;
    }

    public async Task InsertDocumentAsync(string practiceSuffix, string? patientId, string? encounterId, string? messageSubject, string dmsKey, int itemTypeId, DateTime resultDate, string? referralId, string? userId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceSuffix, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", Convert.ToInt32(patientId)),
            new("@pMessageSubject", (object?)messageSubject ?? DBNull.Value),
            new("@pDMSID", dmsKey),
            new("@pInboxItemTypeID", itemTypeId)
        };
        if (!resultDate.Equals(DateTime.MinValue))
        {
            parameters.Add(new SqlParameter("@pResultDate", resultDate));
        }

        parameters.Add(new SqlParameter("@pDataSourceId", "23"));
        if (!string.IsNullOrWhiteSpace(referralId))
        {
            parameters.Add(new SqlParameter("@pReferralId", referralId));
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            parameters.Add(new SqlParameter("@pUserId", userId));
        }

        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            // Legacy's real (misspelled) parameter name.
            parameters.Add(new SqlParameter("@pEnounterId", encounterId));
        }

        var outParam = new SqlParameter("@pOutputParam", SqlDbType.BigInt) { Direction = ParameterDirection.Output, Value = -1 };
        parameters.Add(outParam);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspInsertDocument]", parameters, ct);
    }

    /// <summary>
    /// Legacy `DocumentTypeID` via `Utility.GetBetweenString("/", ";", contentType, false)` - the real
    /// quirk (already ported for KARO): a subtype is only extracted when BOTH "/" and ";" are present,
    /// so a bare "application/pdf" yields an empty search string, which `Contains`-matches the FIRST
    /// configured `DMSDocTypes` entry (PDF, id 1). -1 on any failure, exactly like legacy's catch.
    /// </summary>
    private async Task<int> ResolveDocumentTypeAsync(string? contentType, CancellationToken ct)
    {
        var postedType = string.Empty;
        if (!string.IsNullOrEmpty(contentType) && contentType.Contains('/') && contentType.Contains(';'))
        {
            var start = contentType.IndexOf('/') + 1;
            var end = contentType.IndexOf(';');
            postedType = contentType.Substring(start, end - start).Trim();
        }

        try
        {
            var raw = await _secretProvider.GetSecretAsync("Erms:DMSDocTypes", ct) ?? string.Empty;
            var docTypes = raw.ToLowerInvariant().Split('|');
            var match = docTypes.FirstOrDefault(s => s.Contains(postedType.ToLowerInvariant()));
            return match is null ? -1 : int.Parse(match.Split(',')[0]);
        }
        catch
        {
            return -1;
        }
    }
}
