using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Screening;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Screening;

/// <summary>FLAGGED INFERENCES: procedure/column names follow the same conventions documented on other Block 2 repositories.</summary>
public sealed class ScreeningRepository : IScreeningRepository
{
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public ScreeningRepository(ILegacyPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IReadOnlyList<ScreeningCode>> GetCodesAsync(string practiceId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetScreeningCodes]", null, ct);

        return table.Rows.Cast<DataRow>()
            .Select(row => new ScreeningCode(row["Code"]?.ToString() ?? string.Empty, row["Description"]?.ToString() ?? string.Empty))
            .ToList();
    }

    public async Task<ScreeningCodeResult> SaveAsync(int patientId, int encounterId, string practiceId, ScreeningCodeInput input, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pEncounterID", encounterId),
            new("@pCode", input.Code),
            new("@pValue", (object?)input.Value ?? DBNull.Value)
        };

        var affected = await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspSaveScreeningCode]", parameters, ct);
        return new ScreeningCodeResult(input.Code, input.Value, affected > 0);
    }
}
