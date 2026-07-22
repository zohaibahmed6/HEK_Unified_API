using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Ported from legacy-reference/Hiso/Task.cs. Subject = SNOMED/Read-code lookup result
/// (`Billing.usptblConcept_GetByCode`) + `"&lt;br/&gt;"` + free-text description (confirmed exact
/// concatenation from real source). Status ID and priority/task-type IDs are config-driven
/// (`Hiso:TaskStatusActive`/`Hiso:TaskStatusCompleted`/`Hiso:Acc45TaskTypeTypeId`/`Hiso:TaskPriorityId`),
/// never hardcoded, via ISecretProvider.
/// </summary>
public sealed class HisoTaskRepository : IHisoTaskRepository
{
    private readonly IHisoPracticeConnectionResolver _connectionResolver;
    private readonly ISecretProvider _secretProvider;

    public HisoTaskRepository(IHisoPracticeConnectionResolver connectionResolver, ISecretProvider secretProvider)
    {
        _connectionResolver = connectionResolver;
        _secretProvider = secretProvider;
    }

    public async Task<bool> AddTaskAsync(HisoTaskInput input, string practiceId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);

        var subjectFromCode = await GetConceptNameByReadCodeAsync(connectionString, input.Code, ct);
        var subject = $"{subjectFromCode}<br/>{input.Description}";

        var taskTypeId = await _secretProvider.GetSecretAsync("Hiso:Acc45TaskTypeTypeId", ct);
        var priorityId = await _secretProvider.GetSecretAsync("Hiso:TaskPriorityId", ct);
        var statusId = input.Complete
            ? await _secretProvider.GetSecretAsync("Hiso:TaskStatusCompleted", ct)
            : await _secretProvider.GetSecretAsync("Hiso:TaskStatusActive", ct);

        var parameters = new List<SqlParameter>
        {
            new("@pTaskSubject", subject),
            new("@pProviderID", input.ProviderId),
            new("@pStartDate", input.DueDate.ToDateTime(TimeOnly.MinValue)),
            new("@pEndDate", input.DueDate.ToDateTime(TimeOnly.MinValue)),
            new("@pPracticeID", practiceId),
            new("@pIsShowOnTimeline", true),
            new("@pACC45ID", (object?)input.Acc45Id ?? DBNull.Value),
            new("@pTaskTypeID", (object?)taskTypeId ?? DBNull.Value),
            new("@pIsConfidential", false),
            new("@pUpdatedBy", (object?)input.UpdatedBy ?? DBNull.Value),
            new("@pProirtyID", (object?)priorityId ?? DBNull.Value),
            new("@pLastStatusID", (object?)statusId ?? DBNull.Value),
            new("@pPatientID", input.PatientId),
            new("@pUserLoggingID", 1),
            new("@pAddtoPrompt", false),
            new("@pTaskMedicationIds", string.Empty),
            new("@pDataSourceID", 10),
            new("@pPharmacyID", DBNull.Value),
            new("@pOtherPharmacy", DBNull.Value),
            new("@pIsSelfPickup", false),
            new("@pPrescriberProviderID", DBNull.Value),
            new("@pPrimaryAssignFrom", DBNull.Value),
            new("@pMasterServiceSubServiceID", DBNull.Value),
            new("@pPrice", DBNull.Value),
            new("@pNoCharged", false)
        };

        var affected = await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[Task].[uspAddTaskExternal]", parameters, ct);
        return affected > 0;
    }

    private static async Task<string> GetConceptNameByReadCodeAsync(string connectionString, string readCode, CancellationToken ct)
    {
        var parameters = new List<SqlParameter>
        {
            new("@SNOMEDID", DBNull.Value),
            new("@ReadCode", readCode)
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[Billing].[usptblConcept_GetByCode]", parameters, ct);
        return table.Rows.Count > 0 ? table.Rows[0]["FullySpecifiedName"]?.ToString() ?? string.Empty : string.Empty;
    }
}
