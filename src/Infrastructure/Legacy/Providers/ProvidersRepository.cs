using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Providers;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Providers;

/// <summary>FLAGGED INFERENCES: procedure/column names follow the same conventions documented on other Block 2 repositories.</summary>
public sealed class ProvidersRepository : IProvidersRepository
{
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public ProvidersRepository(ILegacyPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IReadOnlyList<Provider>> GetAsync(string practiceId, string? practiceLocationId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pPracticeLocationID", (object?)practiceLocationId ?? DBNull.Value) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetProvider]", parameters, ct);

        return table.Rows.Cast<DataRow>()
            .Select(row => new Provider(
                row["ProviderId"]?.ToString() ?? string.Empty,
                row["Name"]?.ToString() ?? string.Empty,
                row["PracticeLocationId"] is DBNull or null ? null : row["PracticeLocationId"].ToString()))
            .ToList();
    }
}
