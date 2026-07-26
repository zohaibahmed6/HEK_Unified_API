using System.Data;
using System.Text.Json;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Acc45;
using HekCoreApi.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace HekCoreApi.Infrastructure.Legacy.Acc45;

/// <summary>
/// FLAGGED INFERENCES throughout (procedure/column names, version numbers) - no live schema access.
/// </summary>
public sealed class Acc45Repository : IAcc45Repository
{
    private readonly IHisoConceptExecutor _hisoExecutor;
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;
    private readonly IHisoDocumentHandler _documentHandler;
    private readonly ILogger<Acc45Repository> _logger;

    public Acc45Repository(
        IHisoConceptExecutor hisoExecutor,
        ILegacyPracticeConnectionResolver connectionResolver,
        IHisoDocumentHandler documentHandler,
        ILogger<Acc45Repository> logger)
    {
        _hisoExecutor = hisoExecutor;
        _connectionResolver = connectionResolver;
        _documentHandler = documentHandler;
        _logger = logger;
    }

    public Task<VersionInfo> GetVersionAsync(CancellationToken ct = default) =>
        Task.FromResult(new VersionInfo("HEK Core API - ACC45", "1.0.0-day1-draft", "unknown", 1));

    public async Task<DeliveryOptions> GetDeliveryOptionsAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var parameters = new List<SqlParameter> { new("@pPracticeID", session.PracticeId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "Hiso.uspGetDeliveryOptions", parameters, ct);

        if (table.Rows.Count == 0)
        {
            return new DeliveryOptions(null, null);
        }

        var row = table.Rows[0];
        return new DeliveryOptions(row["Url"]?.ToString(), row["PracticeEdi"]?.ToString());
    }

    public async Task<FormData> GetFormsAsync(Guid sessionKey, HealthLinkSession session, string mode, CancellationToken ct = default)
    {
        var procedureName = mode == "static" ? "Hiso.uspGetPatient_Acc45Form_Static" : "Hiso.uspGetPatient_Acc45Form";
        var dataSet = await _hisoExecutor.ExecuteAsync(procedureName, session, [new HisoRequest { ProcedureName = procedureName }], ct);

        var dataContainer = new Dictionary<string, object?>();
        if (dataSet?.Tables.Count > 0)
        {
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                foreach (DataColumn column in dataSet.Tables[0].Columns)
                {
                    dataContainer[column.ColumnName] = row[column] is DBNull ? null : row[column];
                }
            }
        }

        return new FormData(sessionKey.ToString(), "form", mode, dataContainer, null);
    }

    public async Task<FormData?> GetFormAsync(Guid sessionKey, string formInstanceId, HealthLinkSession session, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var parameters = new List<SqlParameter> { new("@pFormInstanceID", formInstanceId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "Hiso.uspGetFormView", parameters, ct);

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        var dataContainer = row["DataContainer"] is DBNull or null
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(row["DataContainer"].ToString()!) ?? [];

        return new FormData(formInstanceId, row["ViewType"]?.ToString(), row["View"]?.ToString(), dataContainer, null);
    }

    public async Task<FormData> SaveFormAsync(Guid sessionKey, string formInstanceId, HealthLinkSession session, FormSaveInput input, CancellationToken ct = default)
    {
        // HISO-BR-12: render view to DMS, THEN persist the ACC45 definition, DMS GUID linked.
        // Legacy (`FormSessionService.svc.cs:314-321`, `DocumentHandler.AddDocument`) saves the
        // caller-supplied `view`/`viewType` bytes as-is (HTML as UTF8 text, PDF as base64-decoded
        // bytes) - it does NOT run Aspose rendering here at all (that only happens in `getData`'s
        // attachment conversion, see `AsposeMimeConverter`/`IHisoMimeConverter`). Reuses the same real
        // `IHisoDocumentHandler` the SOAP `saveContainer` wire-compat path (`SaveContainerCommand`)
        // already verified against `[dbo].[uspDocumentSave]`, instead of JSON-serializing
        // `DataContainer` (a different field, not what legacy saves as the document).
        var renderedJson = JsonSerializer.Serialize(input.DataContainer);
        var dmsGuid = await _documentHandler.AddDocumentAsync(input.View, input.ViewType ?? "text/html", formInstanceId, session.PracticeId, ct);

        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pFormInstanceID", formInstanceId),
            new("@pPatientID", session.PatientId),
            new("@pAppointmentID", session.AppointmentId),
            new("@pPracticeID", session.PracticeId),
            new("@pDataContainer", renderedJson),
            new("@pView", (object?)input.View ?? DBNull.Value),
            new("@pViewType", (object?)input.ViewType ?? DBNull.Value),
            new("@pCompleted", input.Completed),
            new("@pDmsGuid", dmsGuid.ToString())
        };

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "Hiso.uspSaveAcc45Definition", parameters, ct);

        _logger.LogInformation("ACC45 form {FormInstanceId} saved for session {SessionKey}, DMS GUID {DmsGuid}.", formInstanceId, sessionKey, dmsGuid);
        return new FormData(formInstanceId, input.ViewType, input.View, input.DataContainer, null);
    }

    public async Task<FormActionResult> DispatchActionAsync(Guid sessionKey, HealthLinkSession session, FormActionInput input, CancellationToken ct = default)
    {
        // Legacy addInvoice/launchForm were unimplemented no-op stubs (silently reporting
        // processed=false). Implemented for real per stakeholder Decision 4 - each branch performs
        // a genuine, parameterized operation, not a silent no-op.
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var actionContainerJson = input.ActionContainer is null ? null : JsonSerializer.Serialize(input.ActionContainer);

        var parameters = new List<SqlParameter>
        {
            new("@pSessionKey", sessionKey),
            new("@pPatientID", session.PatientId),
            new("@pAppointmentID", session.AppointmentId),
            new("@pPracticeID", session.PracticeId),
            new("@pActionContainer", (object?)actionContainerJson ?? DBNull.Value)
        };

        var procedureName = input.ActionId switch
        {
            "save" => "Hiso.uspProcessAction_Save",
            "addTask" => "Hiso.uspProcessAction_AddTask",
            "addInvoice" => "Hiso.uspProcessAction_AddInvoice",
            "launchForm" => "Hiso.uspProcessAction_LaunchForm",
            _ => throw new ConflictException($"Unknown action '{input.ActionId}'.")
        };

        var affected = await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, procedureName, parameters, ct);
        return new FormActionResult(affected > 0);
    }
}
