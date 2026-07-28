using System.Data;
using System.Text;
using System.Text.Json;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>Ported from `HSSDA.cs`'s remaining real save/insert methods.</summary>
public sealed class KaroWriteRepository : IKaroWriteRepository
{
    private readonly IKaroPracticeConnectionResolver _connectionResolver;
    private readonly IKaroDmsConnectionResolver _dmsConnectionResolver;
    private readonly ISecretProvider _secretProvider;

    public KaroWriteRepository(IKaroPracticeConnectionResolver connectionResolver, IKaroDmsConnectionResolver dmsConnectionResolver, ISecretProvider secretProvider)
    {
        _connectionResolver = connectionResolver;
        _dmsConnectionResolver = dmsConnectionResolver;
        _secretProvider = secretProvider;
    }

    public async Task<long> SaveClinicalNotesAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? appointmentId, string? userId, string? subjective, string? objective, string? assessment, string? plans, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter>
        {
            new("@pPatientId", int.Parse(patientId!)),
            new("@pAppointmentId", long.Parse(appointmentId!))
        };
        if (!string.IsNullOrWhiteSpace(userId))
        {
            parameters.Add(new SqlParameter("@pUserId", int.Parse(userId)));
        }

        AddIfPresent(parameters, "@pSubjective", subjective);
        AddIfPresent(parameters, "@pObjective", objective);
        AddIfPresent(parameters, "@pAssessment", assessment);
        AddIfPresent(parameters, "@pPlans", plans);

        var outParam = new SqlParameter("@pOutputParam", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
        parameters.Add(outParam);

        await RunAsync(routingContext, "[HSS].[uspInsertUpdateConsultNotes]", parameters, ct);
        return outParam.Value is DBNull or null ? 0 : Convert.ToInt64(outParam.Value);
    }

    public async Task<int> SaveConditionAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? appointmentId, string? userId, string? diagnosisType, DateTime? onsetDate, string? summary, bool isLongTerm, string? conceptId, string? diseaseName, string? fsn, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter>
        {
            new("@pPatientId", int.Parse(patientId!)),
            new("@pAppointmentId", long.Parse(appointmentId!))
        };
        AddIfPresent(parameters, "@pDiagnosisType", diagnosisType);
        if (onsetDate is { } date)
        {
            parameters.Add(new SqlParameter("@pOnsetDate", date));
        }

        AddIfPresent(parameters, "@pSummary", summary);
        AddIfPresent(parameters, "@pConceptId", conceptId);
        AddIfPresent(parameters, "@pDiseaseName", diseaseName);
        AddIfPresent(parameters, "@pFSN", fsn);
        parameters.Add(new SqlParameter("@pIsLongTerm", isLongTerm));

        var outParam = new SqlParameter("@pOutputParam", SqlDbType.Int) { Direction = ParameterDirection.Output };
        parameters.Add(outParam);

        await RunAsync(routingContext, "[HSS].[uspInsertUpdateDiagnosis]", parameters, ct);
        return outParam.Value is DBNull or null ? 0 : Convert.ToInt32(outParam.Value);
    }

    /// <summary>Legacy: `locationId` is always the literal "167" - not derived from any request field.</summary>
    public async Task<int> SaveInvoiceAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? encounterId, string? name, string? code, string? fee, string? userId, string? payee, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", int.Parse(patientId!)),
            new("@pAppointmentId", encounterId!),
            new("@pMasterServiceName", "HSS Service"),
            new("@pSubServiceName", name!)
        };
        AddIfPresent(parameters, "@pSubServiceCode", code);
        AddIfPresent(parameters, "@pFee", fee);
        parameters.Add(new SqlParameter("@pLocationId", "167"));
        AddIfPresent(parameters, "@pUserId", userId);
        AddIfPresent(parameters, "@pPayee", payee);

        var outParam = new SqlParameter("@pOutputParam", SqlDbType.Int) { Direction = ParameterDirection.Output };
        parameters.Add(outParam);

        await RunAsync(routingContext, "[HSS].[uspInsertUpdateService]", parameters, ct);
        return outParam.Value is DBNull or null ? 0 : Convert.ToInt32(outParam.Value);
    }

    public async Task<int> SaveObservationsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? appointmentId, string? userId, string? temperature, string? waist, string? height, string? weight, string? bpSys, string? bpDia, string? heartRate, string? notes, string? risk, string? framingham, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter>
        {
            new("@pPatientId", int.Parse(patientId!)),
            new("@pAppointmentId", long.Parse(appointmentId!))
        };
        AddIfPresent(parameters, "@pTemperature", temperature);
        AddIfPresent(parameters, "@pWaist", waist);
        AddIfPresent(parameters, "@pHeight", height);
        AddIfPresent(parameters, "@pWeight", weight);
        AddIfPresent(parameters, "@pBPSys", bpSys);
        AddIfPresent(parameters, "@pBPDia", bpDia);
        AddIfPresent(parameters, "@pHeartRate", heartRate);
        AddIfPresent(parameters, "@pNotes", notes);
        AddIfPresent(parameters, "@pRisk", risk);
        AddIfPresent(parameters, "@pFramingham", framingham);

        var outParam = new SqlParameter("@pOutputParam", SqlDbType.Int) { Direction = ParameterDirection.Output };
        parameters.Add(outParam);

        await RunAsync(routingContext, "[HSS].[uspInsertUpdateObservation]", parameters, ct);
        return outParam.Value is DBNull or null ? 0 : Convert.ToInt32(outParam.Value);
    }

    public async Task<int> SaveRecallAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? appointmentId, string? userId, string? priority, string? group, DateTime? dueDate, string? notes, string? categoryId, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter>
        {
            new("@pPatientId", int.Parse(patientId!)),
            new("@pAppointmentId", long.Parse(appointmentId!))
        };
        AddIfPresent(parameters, "@pPriority", priority);
        AddIfPresent(parameters, "@pGroup", group);
        if (dueDate is { } date)
        {
            parameters.Add(new SqlParameter("@pDueDate", date));
        }

        AddIfPresent(parameters, "@pNotes", notes);
        AddIfPresent(parameters, "@pUserId", userId);
        AddIfPresent(parameters, "@pRecallCategoryId", categoryId);

        var outParam = new SqlParameter("@pOutputParam", SqlDbType.Int) { Direction = ParameterDirection.Output };
        parameters.Add(outParam);

        await RunAsync(routingContext, "[HSS].[uspInsertUpdateRecall]", parameters, ct);
        return outParam.Value is DBNull or null ? 0 : Convert.ToInt32(outParam.Value);
    }

    /// <summary>Ported from `SaveToDMS` (`[dbo].[uspDocumentSave]` on the real DMS connection) + `InsertDocument` (`[HSS].[uspInsertDocument]` on the real Indici connection).</summary>
    public async Task<KaroSaveDocumentResult> SaveDocumentAsync(string practiceSuffix, string practiceSuffixNumeric, RoutingContext routingContext, string? patientId, string? encounterId, byte[]? messageData, string? contentType, string? messageSubject, string? itemType, CancellationToken ct = default)
    {
        var isInbound = string.Equals(itemType, "in", StringComparison.OrdinalIgnoreCase);
        var categoryId = isInbound ? 17 : 18;

        var postedType = await ResolveDocumentTypeAsync(contentType, ct);
        var dmsConnectionString = await _dmsConnectionResolver.ResolveAsync(routingContext, ct);

        var dmsParameters = new List<SqlParameter>
        {
            new("@pClientID", 3),
            new("@pCategoryID", categoryId),
            new("@pDocumentName", messageSubject ?? string.Empty),
            new("@pDocumentTypeID", postedType),
            new("@pDescription", "HSS"),
            new("@pDocumentKey", Guid.NewGuid().ToString()),
            new("@pDocumentSize", messageData?.Length ?? 0),
            new("@pProfileID", "1"),
            new("@pPracticeID", practiceSuffixNumeric),
            new("@pDocumentData", SqlDbType.VarBinary) { Value = (object?)messageData ?? DBNull.Value }
        };
        var documentKey = (string)dmsParameters[5].Value!;

        var dmsOutParam = new SqlParameter("@pDocumentIDOut", SqlDbType.Int) { Direction = ParameterDirection.Output, Value = -1 };
        dmsParameters.Add(dmsOutParam);

        await LegacyDbExecutor.ExecuteNonQueryAsync(dmsConnectionString, CommandType.StoredProcedure, "[dbo].[uspDocumentSave]", dmsParameters, ct);
        var dmsId = dmsOutParam.Value is DBNull or null ? -1 : Convert.ToInt32(dmsOutParam.Value);
        if (dmsId <= 0)
        {
            documentKey = string.Empty;
        }

        if (string.IsNullOrEmpty(documentKey))
        {
            return new KaroSaveDocumentResult(false, null);
        }

        var indiciConnectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        var insertParameters = new List<SqlParameter>
        {
            new("@pPatientID", int.Parse(patientId!)),
            new("@pMessageSubject", messageSubject ?? string.Empty),
            new("@pDMSID", documentKey),
            new("@pInboxItemTypeID", isInbound ? 1 : 2),
            new("@pDataSourceId", "25")
        };
        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            insertParameters.Add(new SqlParameter("@pEnounterId", encounterId));
        }

        var insertOutParam = new SqlParameter("@pOutputParam", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
        insertParameters.Add(insertOutParam);

        await LegacyDbExecutor.ExecuteNonQueryAsync(indiciConnectionString, CommandType.StoredProcedure, "[HSS].[uspInsertDocument]", insertParameters, ct);
        var saveDoc = insertOutParam.Value is DBNull or null ? 0 : Convert.ToInt64(insertOutParam.Value);

        return new KaroSaveDocumentResult(saveDoc > 0, documentKey);
    }

    /// <summary>
    /// Legacy: `Utility.GetBetweenString("/", ";", contentType, false)` - real quirk: returns empty
    /// string unless BOTH "/" and ";" are present in the content type (a plain "image/png" with no
    /// trailing ";" yields an EMPTY postedType). `DocumentTypeID` then does a `Contains`-based first
    /// match against the real `DMSDocTypes` appSetting list - an empty search string matches every
    /// entry, so the real (quirky, not "fixed") result for a bare "image/png" is the FIRST configured
    /// type (PDF, id 1). Reproduced exactly.
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
            var raw = await _secretProvider.GetSecretAsync("Karo:DMSDocTypes", ct) ?? string.Empty;
            var docTypes = raw.ToLowerInvariant().Split('|');
            var match = docTypes.FirstOrDefault(s => s.Contains(postedType));
            return match is null ? -1 : int.Parse(match.Split(',')[0]);
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>Ported from `GetTemplateSchema`/`CreateDataTable`/`FillSummaryData`/`BuildJsonTimeLineData`/`InsertSummary` (`SaveSummary`, `APIController.cs:978`).</summary>
    public async Task<int> SaveSummaryAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? encounterId, string? providerId, string? identifier, string? dateTimeRecorded, string entriesJson, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        var schema = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetTemplateSchema]", BuildSchemaParams(patientId, identifier), ct);
        if (schema.Rows.Count == 0)
        {
            // Legacy: real message here is "Invalid values passed!" (distinct from InsertSummary's own -4 "Invalid Identifier" sentinel).
            return KaroSaveSummarySentinels.SchemaNotFound;
        }

        var summary = CreateSummaryTable();
        using var document = JsonDocument.Parse(entriesJson);

        foreach (var entryElement in document.RootElement.EnumerateArray())
        {
            if (!FillSummaryRow(schema, summary, identifier, dateTimeRecorded, entryElement))
            {
                // Legacy: real message here is "Error Parsing the JSON Data..." (distinct from InsertSummary's sentinels).
                return KaroSaveSummarySentinels.ParseFailed;
            }
        }

        if (summary.Rows.Count == 0)
        {
            return KaroSaveSummarySentinels.ParseFailed;
        }

        foreach (DataRow row in summary.Rows)
        {
            foreach (DataColumn column in summary.Columns)
            {
                if (string.IsNullOrEmpty(row[column]?.ToString()))
                {
                    row[column] = DBNull.Value;
                }
            }
        }

        var firstRow = summary.Rows[0];
        var outcome = firstRow["Outcome"]?.ToString();
        var ds = firstRow["DS"]?.ToString();
        var onset = firstRow["ONSET"]?.ToString();
        summary.Columns.Remove("Outcome");
        summary.Columns.Remove("DS");
        summary.Columns.Remove("ONSET");

        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrEmpty(patientId))
        {
            parameters.Add(new SqlParameter("@pPatientID", int.Parse(patientId)));
        }

        if (!string.IsNullOrEmpty(encounterId))
        {
            parameters.Add(new SqlParameter("@pEncounterID", int.Parse(encounterId)));
        }

        if (!string.IsNullOrEmpty(providerId))
        {
            parameters.Add(new SqlParameter("@pProviderID", int.Parse(providerId)));
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            parameters.Add(new SqlParameter("@pIdentifier", identifier));
        }

        if (!string.IsNullOrEmpty(outcome))
        {
            parameters.Add(new SqlParameter("@pOutCome", outcome));
        }

        if (!string.IsNullOrEmpty(ds) && DateTime.TryParse(ds, out var dsDate))
        {
            parameters.Add(new SqlParameter("@pDS", dsDate));
        }

        if (!string.IsNullOrEmpty(onset) && DateTime.TryParse(onset, out var onsetDate))
        {
            parameters.Add(new SqlParameter("@pOnSet", onsetDate));
        }

        parameters.Add(new SqlParameter("@pScreeningUDT", summary));
        var outParam = new SqlParameter("@outputparam", SqlDbType.Int) { Direction = ParameterDirection.Output, Value = -1 };
        parameters.Add(outParam);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspInsertSummary]", parameters, ct);
        return outParam.Value is DBNull or null ? -1 : Convert.ToInt32(outParam.Value);
    }

    private static List<SqlParameter> BuildSchemaParams(string? patientId, string? identifier)
    {
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrEmpty(patientId))
        {
            parameters.Add(new SqlParameter("@pPatientID", patientId));
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            parameters.Add(new SqlParameter("@pIdentifier", identifier));
        }

        return parameters;
    }

    private static DataTable CreateSummaryTable()
    {
        var table = new DataTable();
        for (var i = 1; i <= 50; i++)
        {
            table.Columns.Add("Field" + i, typeof(string));
        }

        table.Columns.Add("PCode", typeof(string));
        table.Columns.Add("PCode2", typeof(string));
        table.Columns.Add("JsonData", typeof(string));
        table.Columns.Add("HtmlData", typeof(string));
        table.Columns.Add("EffectiveDateTime", typeof(DateTime));
        table.Columns.Add("IsConfidential", typeof(bool));
        table.Columns.Add("IsPatientPortal", typeof(bool));
        table.Columns.Add("Outcome", typeof(string));
        table.Columns.Add("DS", typeof(string));
        table.Columns.Add("ONSET", typeof(string));
        return table;
    }

    private static readonly string[] ExcludedFields = { "OUTCOME", "DS", "ONSET" };

    /// <summary>Legacy: `FillSummaryData` + `BuildJsonTimeLineData` - one summary row per top-level `entries[]` element, each element's own properties become the row's fields.</summary>
    private static bool FillSummaryRow(DataTable schema, DataTable summary, string? identifier, string? dateTimeRecorded, JsonElement entry)
    {
        var row = summary.NewRow();
        var posted = new List<(string OriginalName, string? NameWithoutIdentifier, string? FieldId, string? FieldColumnDB, string? NameWithSpace, string Value, string Type)>();

        foreach (var property in entry.EnumerateObject())
        {
            var originalName = property.Name;
            var comparison = schema.Select($"FieldCaption = '{originalName.Replace("'", "''")}'");

            var name = originalName;
            string? nameWithoutIdentifier = null;
            if (name.Contains('_'))
            {
                nameWithoutIdentifier = name = name[(name.IndexOf('_') + 1)..];
            }

            if (comparison.Length == 0 && !ExcludedFields.Contains(name.ToUpperInvariant()))
            {
                continue;
            }

            var dbType = string.Empty;
            if (!ExcludedFields.Contains(name.ToUpperInvariant()))
            {
                dbType = comparison[0]["ScnFieldType"]?.ToString()?.Replace("N", "integer").Replace("B", "boolean").Replace("T", "string") ?? string.Empty;
            }

            var valueText = ValueToString(property.Value);
            if (name.ToUpperInvariant().Contains("DATE") && DateTime.TryParse(valueText, out var dt) && dt > DateTime.MinValue)
            {
                valueText = dt.ToString("dd/MM/yyyy").Replace("-", "/");
            }

            if (ExcludedFields.Contains(name.ToUpperInvariant()))
            {
                row[name] = ValueToString(property.Value);
                continue;
            }

            if (string.IsNullOrEmpty(dbType))
            {
                continue;
            }

            var jsonType = property.Value.ValueKind switch
            {
                JsonValueKind.Number => property.Value.TryGetInt64(out _) ? "integer" : "float",
                JsonValueKind.True or JsonValueKind.False => "boolean",
                _ => "string"
            };

            if (jsonType is "integer" or "float")
            {
                if (!jsonType.Equals(dbType, StringComparison.OrdinalIgnoreCase) && !jsonType.Equals("float", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            else if (jsonType == "boolean" && !jsonType.Equals(dbType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fieldColumnDb = comparison[0]["FieldName"]?.ToString();
            var nameWithSpace = comparison[0]["FieldCaption"]?.ToString();
            var fieldId = comparison[0]["FieldId"]?.ToString();

            posted.Add((originalName, nameWithoutIdentifier, fieldId, fieldColumnDb, nameWithSpace, valueText, jsonType));
        }

        var dbFields = schema.Rows.Cast<DataRow>().Select(r => r["FieldCaption"]?.ToString() ?? string.Empty).ToList();
        var (json, html) = BuildJsonTimeline(posted, dbFields, row, summary);

        row["PCode"] = string.Empty;
        row["PCode2"] = string.Empty;
        row["JsonData"] = json;
        row["HtmlData"] = $"<b>{identifier}:</b>\r" + html.Replace("true", "Yes").Replace("false", "No");

        var recorded = string.IsNullOrEmpty(dateTimeRecorded) ? DateTime.Now.ToShortDateString() : dateTimeRecorded;
        row["EffectiveDateTime"] = DateTime.TryParse(recorded, out var effectiveDate) ? effectiveDate : DateTime.MinValue.Date;
        row["IsConfidential"] = false;
        row["IsPatientPortal"] = false;

        summary.Rows.Add(row);
        return true;
    }

    private static string ValueToString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => string.Empty
    };

    private static (string Json, string Html) BuildJsonTimeline(
        List<(string OriginalName, string? NameWithoutIdentifier, string? FieldId, string? FieldColumnDB, string? NameWithSpace, string Value, string Type)> posted,
        List<string> dbFields, DataRow row, DataTable summary)
    {
        var json = new StringBuilder("[");
        var timeline = new StringBuilder();

        foreach (var field in dbFields)
        {
            var data = posted.FirstOrDefault(p => string.Equals(p.OriginalName, field, StringComparison.OrdinalIgnoreCase));
            if (data.OriginalName is null)
            {
                continue;
            }

            json.Append("\r{\r");
            json.Append($"\"{data.OriginalName}\" : \"{data.Value}\",\r");
            timeline.Append($"{data.OriginalName} : {data.Value},\r");

            switch (data.Type)
            {
                case "string":
                    json.Append($"\"value\" :  \"{data.Value}\",\r");
                    break;
                case "boolean":
                    var boolValue = Convert.ToBoolean(data.Value);
                    json.Append($"\"id\":\"{data.NameWithoutIdentifier}-0\",\r");
                    json.Append($"\"{data.NameWithoutIdentifier}_selected\":\"{data.NameWithSpace}\",\r");
                    json.Append($"\"Status\":{boolValue.ToString().ToLowerInvariant()},\r");
                    json.Append(boolValue ? "\"value\" : \"Ticked\",\r" : "\"value\" : \"\",\r");
                    break;
                case "integer":
                    json.Append($"\"value\" : {Convert.ToInt64(data.Value)},\r");
                    break;
                case "float":
                    json.Append($"\"value\" : {data.Value},\r");
                    break;
            }

            json.Append($"\"label\" :  \"{data.NameWithSpace}\",\r");
            json.Append($"\"FieldID\" :  \"{data.FieldId}\"\r}},");

            if (data.FieldColumnDB is not null && summary.Columns.Contains(data.FieldColumnDB))
            {
                row[data.FieldColumnDB] = data.Type == "boolean" ? Convert.ToBoolean(data.Value).ToString().ToLowerInvariant() : data.Value;
            }
        }

        var jsonResult = json.ToString().TrimEnd(',') + "\r]";
        return (jsonResult, timeline.ToString().TrimEnd(','));
    }

    private async Task RunAsync(RoutingContext routingContext, string procedureName, List<SqlParameter> parameters, CancellationToken ct)
    {
        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, procedureName, parameters, ct);
    }

    private static void AddIfPresent(List<SqlParameter> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters.Add(new SqlParameter(name, value));
        }
    }
}
