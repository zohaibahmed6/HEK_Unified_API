using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Recalls;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Recalls;

/// <summary>FLAGGED INFERENCES: procedure/column names follow the same conventions documented on other Block 2 repositories.</summary>
public sealed class RecallsRepository : IRecallsRepository
{
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public RecallsRepository(ILegacyPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IReadOnlyList<RecallCategory>> GetCategoriesAsync(string practiceId, string? group, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pGroup", (object?)group ?? DBNull.Value) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetRecallCategories]", parameters, ct);

        return table.Rows.Cast<DataRow>()
            .Select(row => new RecallCategory(row["CategoryId"]?.ToString() ?? string.Empty, row["Name"]?.ToString() ?? string.Empty))
            .ToList();
    }

    public async Task<IReadOnlyList<Recall>> GetForPatientAsync(int patientId, string practiceId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pPatientID", patientId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetRecalls]", parameters, ct);

        return table.Rows.Cast<DataRow>()
            .Select(row => new Recall(
                row["RecallId"]?.ToString() ?? Guid.NewGuid().ToString(),
                patientId,
                row["CategoryId"] is DBNull or null ? null : row["CategoryId"].ToString(),
                DateOnly.FromDateTime(Convert.ToDateTime(row["DueDate"]))))
            .ToList();
    }

    public async Task<Recall> SaveAsync(int patientId, string practiceId, RecallInput input, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pCategoryID", (object?)input.CategoryId ?? DBNull.Value),
            new("@pDueDate", input.DueDate.ToDateTime(TimeOnly.MinValue))
        };

        var output = new SqlParameter("@pRecallIDOut", SqlDbType.NVarChar, 64) { Direction = ParameterDirection.Output };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspSaveRecall]", parameters, ct);

        return new Recall(output.Value?.ToString() ?? Guid.NewGuid().ToString(), patientId, input.CategoryId, input.DueDate);
    }
}
