using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.PracticeContext;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.PracticeContext;

/// <summary>FLAGGED INFERENCES: procedure/column names follow the same conventions documented on other Block 2 repositories. COL/Pegasus is confirmed undocumented (SRS Section 4.3) - flagged more heavily than other groups.</summary>
public sealed class PracticeContextRepository : IPracticeContextRepository
{
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public PracticeContextRepository(ILegacyPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<PracticeSessionContext?> GetAsync(string practiceId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pPracticeID", practiceId) };

        var surgeryTable = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetSurgeryData]", parameters, ct);
        var sessionTable = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetSessionData]", parameters, ct);

        if (surgeryTable.Rows.Count == 0 && sessionTable.Rows.Count == 0)
        {
            return null;
        }

        return new PracticeSessionContext(practiceId, ToDictionary(surgeryTable), ToDictionary(sessionTable));
    }

    private static Dictionary<string, object?> ToDictionary(DataTable table)
    {
        var result = new Dictionary<string, object?>();
        if (table.Rows.Count == 0)
        {
            return result;
        }

        var row = table.Rows[0];
        foreach (DataColumn column in table.Columns)
        {
            result[column.ColumnName] = row[column] is DBNull ? null : row[column];
        }

        return result;
    }
}
