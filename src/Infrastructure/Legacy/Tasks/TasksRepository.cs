using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Contracts.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Infrastructure.Legacy.Tasks;

/// <summary>FLAGGED INFERENCES: procedure/column names follow the same conventions documented on other Block 2 repositories.</summary>
public sealed class TasksRepository : ITasksRepository
{
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;
    private readonly TaskStatusOptions _statusOptions;

    public TasksRepository(ILegacyPracticeConnectionResolver connectionResolver, IOptions<TaskStatusOptions> statusOptions)
    {
        _connectionResolver = connectionResolver;
        _statusOptions = statusOptions.Value;
    }

    public async Task<TaskResult> CreateAsync(int patientId, string practiceId, TaskInput input, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);

        // HISO-BR-19: subject = resolved concept name (not the raw code) + free-text description.
        var conceptName = await ResolveConceptNameAsync(connectionString, input.ConceptCode, ct);
        var subject = $"{conceptName}<br/>{input.Description}";

        // HISO-BR-20: status via configured status IDs, not hardcoded values.
        var statusId = string.Equals(input.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            ? _statusOptions.CompletedStatusId
            : _statusOptions.ActiveStatusId;

        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pSubject", subject),
            new("@pStatusID", statusId)
        };

        var output = new SqlParameter("@pTaskIDOut", SqlDbType.NVarChar, 64) { Direction = ParameterDirection.Output };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "Hiso.uspAddTask", parameters, ct);

        return new TaskResult(output.Value?.ToString() ?? Guid.NewGuid().ToString(), subject, statusId == _statusOptions.CompletedStatusId ? "Completed" : "Active");
    }

    private static async Task<string> ResolveConceptNameAsync(string connectionString, string conceptCode, CancellationToken ct)
    {
        var parameters = new List<SqlParameter> { new("@pConceptCode", conceptCode) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "Hiso.uspGetConceptName", parameters, ct);
        return table.Rows.Count > 0 ? table.Rows[0]["ConceptName"]?.ToString() ?? conceptCode : conceptCode;
    }
}
