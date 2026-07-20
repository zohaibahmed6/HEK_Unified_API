using System.Data;
using System.Text.Json;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.EncounterSummary;
using HekCoreApi.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.EncounterSummary;

/// <summary>FLAGGED INFERENCES: procedure/column names follow the same conventions documented on other Block 2 repositories.</summary>
public sealed class EncounterSummaryRepository : IEncounterSummaryRepository
{
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public EncounterSummaryRepository(ILegacyPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<TemplateSchema?> GetTemplateSchemaAsync(string practiceId, string identifier, CancellationToken ct = default)
    {
        // FLAGGED: no source document confirms whether the template-schema dictionary
        // (KARO-BR-10) lives per-practice or platform-wide; resolved via the caller's own practice
        // connection for consistency with every other repository rather than inventing a separate
        // platform-DB concept not described anywhere.
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(
            connectionString,
            CommandType.StoredProcedure,
            "[HSS].[uspGetTemplateSchema]",
            [new SqlParameter("@pIdentifier", identifier)],
            ct);

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var fields = table.Rows.Cast<DataRow>()
            .Select(row => new TemplateField(row["Name"]?.ToString() ?? string.Empty, row["Caption"]?.ToString() ?? string.Empty, row["Type"]?.ToString() ?? "string"))
            .ToList();

        return new TemplateSchema(identifier, fields);
    }

    public async Task<EncounterSummaryData?> GetSummaryAsync(int patientId, int encounterId, string practiceId, string identifier, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pEncounterID", encounterId),
            new("@pIdentifier", identifier)
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetEncounterSummary]", parameters, ct);
        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        var fields = row["Fields"] is DBNull or null
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(row["Fields"].ToString()!) ?? [];

        return new EncounterSummaryData(identifier, fields);
    }

    public async Task<EncounterSummaryData> SaveSummaryAsync(int patientId, int encounterId, string practiceId, EncounterSummaryInput input, CancellationToken ct = default)
    {
        var schema = await GetTemplateSchemaAsync(practiceId, input.Identifier, ct)
            ?? throw new NotFoundException($"Template '{input.Identifier}' is not registered.");

        ValidateFieldTypes(schema, input.Fields);

        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pEncounterID", encounterId),
            new("@pIdentifier", input.Identifier),
            new("@pFields", JsonSerializer.Serialize(input.Fields))
        };

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspSaveSummary]", parameters, ct);
        return new EncounterSummaryData(input.Identifier, input.Fields);
    }

    /// <summary>KARO-BR-11: a posted field's declared type must match the schema's expected DB type, except Float is always accepted where integer was expected.</summary>
    private static void ValidateFieldTypes(TemplateSchema schema, IDictionary<string, object?> fields)
    {
        var schemaByName = schema.Fields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (fieldName, value) in fields)
        {
            if (value is null || !schemaByName.TryGetValue(fieldName, out var fieldDef))
            {
                continue;
            }

            var isValid = fieldDef.Type.ToLowerInvariant() switch
            {
                "integer" or "int" => value is int or long or float or double, // Float always accepted where integer expected (KARO-BR-11).
                "float" or "double" or "decimal" => value is float or double or decimal,
                "boolean" or "bool" => value is bool,
                _ => true // string and unrecognized types accepted as-is.
            };

            if (!isValid)
            {
                throw new Domain.Exceptions.ConflictException($"Field '{fieldName}' does not match the expected type '{fieldDef.Type}'.");
            }
        }
    }
}
