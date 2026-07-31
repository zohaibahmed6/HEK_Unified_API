using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Ported from legacy-reference/Hiso/PatientBuilder.cs, PatientConsultBuilder.cs,
/// PatientEmployerOrganisationBuilder.cs, PatientProblemBuilder.cs, RegisteredPractitionerBuilder.cs.
/// Each `UDT_tbl*` column list is real, confirmed config (`Utitlity.GetColumnNameByTableName` reads
/// `AppSettings["UDT_tbl*"]`, a comma-separated column list) - sourced here via
/// <see cref="ISecretProvider"/> under the same key names, never hardcoded.
/// </summary>
public sealed class HisoProcessActionSaveRepository : IHisoProcessActionSaveRepository
{
    private readonly IHisoPracticeConnectionResolver _connectionResolver;
    private readonly ILegacyPracticeConnectionResolver _indiciMasterResolver;
    private readonly ISecretProvider _secretProvider;

    public HisoProcessActionSaveRepository(
        IHisoPracticeConnectionResolver connectionResolver,
        ILegacyPracticeConnectionResolver indiciMasterResolver,
        ISecretProvider secretProvider)
    {
        _connectionResolver = connectionResolver;
        _indiciMasterResolver = indiciMasterResolver;
        _secretProvider = secretProvider;
    }

    public async Task SavePatientAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var mapping = await GetConceptMappingTableAsync(connectionString, ct);
        var referenceId = ResolveReferenceIdOrFallback(session, session.PatientId);
        var table = await BuildSectionFieldTableAsync("UDT_tblPatient", submittedDataXml, mapping, referenceId, ct);

        var parameters = BuildStandardParameters("@tblPatient", table, session);
        await ExecuteWithOutParamAsync(connectionString, "[Hiso].[uspPatient_Update]", parameters, "Failed to update patient info", ct);
    }

    public async Task SavePatientConsultAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var mapping = await GetConceptMappingTableAsync(connectionString, ct);
        var referenceId = ResolveReferenceIdOrFallback(session, session.AppointmentId);
        var table = await BuildSectionFieldTableAsync("UDT_tblPatientConsult", submittedDataXml, mapping, referenceId, ct);

        var parameters = BuildStandardParameters("@tblPatientConsult", table, session);
        await ExecuteWithOutParamAsync(connectionString, "[Hiso].[uspPatientConsult_Update]", parameters, "Failed to update patient consult info", ct);
    }

    public async Task SavePatientProblemAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var mapping = await GetConceptMappingTableAsync(connectionString, ct);
        var columns = await GetColumnsAsync("UDT_tblPatientProblem", ct);

        var table = new DataTable();
        foreach (var column in columns)
        {
            table.Columns.Add(column);
        }

        if (!table.Columns.Contains("DiagnosisID"))
        {
            table.Columns.Add("DiagnosisID");
        }

        var doc = new XmlDocument();
        doc.LoadXml(submittedDataXml);
        // Namespace/prefix-agnostic, matching the `ParseRequest` fix - see the comment further down in
        // this file (real `submittedData` always carries a namespace/prefix plain `SelectNodes` misses).
        var groups = doc.DocumentElement?.GetElementsByTagName("group", "*");
        if (groups is not null)
        {
            foreach (XmlNode group in groups)
            {
                var conceptName = group.Attributes?["conceptName"]?.Value;
                if (!string.Equals(conceptName, "patient_problem", StringComparison.OrdinalIgnoreCase) || group.ChildNodes.Count == 0)
                {
                    continue;
                }

                var row = table.NewRow();
                row["DiagnosisID"] = session.ReferenceId is { Length: > 0 } refId ? refId : 0;
                foreach (XmlNode field in group.ChildNodes)
                {
                    var attrName = field.Attributes?["conceptName"]?.Value;
                    if (string.IsNullOrEmpty(attrName))
                    {
                        continue;
                    }

                    var columnName = ResolveColumnName(mapping, attrName);
                    if (columnName is not null && table.Columns.Contains(columnName))
                    {
                        row[columnName] = field.InnerText;
                    }
                }

                table.Rows.Add(row);
            }
        }

        var parameters = BuildStandardParameters("@tblProblems", table, session);
        await ExecuteWithOutParamAsync(connectionString, "[Hiso].[uspPatientProblem_Add]", parameters, "Failed to update patient problem info", ct);
    }

    public async Task SaveRegisteredPractitionerAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var mapping = await GetConceptMappingTableAsync(connectionString, ct);
        var referenceId = ResolveReferenceIdOrFallback(session, session.ProviderId);
        var table = await BuildSectionFieldTableAsync("UDT_tblRegisteredPractitioner", submittedDataXml, mapping, referenceId, ct);

        var parameters = BuildStandardParameters("@tblRegisteredPractitioner", table, session);
        await ExecuteWithOutParamAsync(connectionString, "[Hiso].[uspRegisteredPractitioner_Update]", parameters, "Failed to update registered practitioner info", ct);
    }

    /// <summary>Legacy: PatientEmployerOrganisationBuilder swallows all exceptions (log-only) - never throws here, matching that behavior.</summary>
    public async Task SavePatientEmployerOrganisationAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default)
    {
        try
        {
            var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
            var mapping = await GetConceptMappingTableAsync(connectionString, ct);
            var columns = await GetColumnsAsync("UDT_tblPatientEmployerOrganisation", ct);

            var table = new DataTable();
            foreach (var column in columns)
            {
                table.Columns.Add(column);
            }

            var row = table.NewRow();
            foreach (DataColumn column in table.Columns)
            {
                row[column] = DBNull.Value;
            }

            var doc = new XmlDocument();
            doc.LoadXml(submittedDataXml);
            var fields = doc.DocumentElement?.GetElementsByTagName("field", "*");
            if (fields is not null)
            {
                foreach (XmlNode field in fields)
                {
                    var conceptName = field.Attributes?["conceptName"]?.Value;
                    if (string.IsNullOrEmpty(conceptName))
                    {
                        continue;
                    }

                    var columnName = ResolveColumnName(mapping, conceptName);
                    if (columnName is not null && table.Columns.Contains(columnName))
                    {
                        row[columnName] = field.InnerText;
                    }
                }
            }

            table.Rows.Add(row);

            int countryId = 0, cityId = 0, suburbId = 0, designationId = 0;
            try
            {
                countryId = await SaveCountryAsync(table, session, ct);
                if (countryId > 0)
                {
                    cityId = await SaveCityAsync(table, session, countryId, ct);
                }

                if (cityId > 0)
                {
                    suburbId = await SaveSuburbAsync(table, session, cityId, ct);
                }

                designationId = await SaveDesignationAsync(table, session, ct);
            }
            catch
            {
                // Legacy: lookup failures here are logged only, not rethrown.
            }

            var parameters = new List<SqlParameter>
            {
                new("@tblOrganization", table),
                new("@pPatientId", session.PatientId),
                new("@pAppointmentId", session.AppointmentId),
                new("@pPracticeId", session.PracticeId),
                new("@pUpdatedBy", session.ProviderId),
                new("@pUserLoggingID", -1),
                new("@pEmployerCityId", cityId),
                new("@pSuburbTownID", suburbId),
                new("@pCountryId", countryId),
                new("@pDesignationID", designationId)
            };

            await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[Hiso].[uspPatientEmployerOrganisation_AddUpdate]", parameters, ct);
        }
        catch
        {
            // Legacy: PatientEmployerOrganisationBuilder.Save/GenerateTable swallow everything (log-only).
        }
    }

    private async Task<int> SaveCountryAsync(DataTable table, HealthLinkSession session, CancellationToken ct)
    {
        var countryName = GetColumnValue(table, "PatientEmployerOrganisation_PhysicalCountry");
        if (string.IsNullOrEmpty(countryName))
        {
            return 0;
        }

        return await RunLookupUpsertAsync(session, "Lookup.uspCountryInsertUpdate", new List<SqlParameter>
        {
            new("@pCountryID", 0),
            new("@pCountryName", countryName),
            new("@pHL7Code", DBNull.Value),
            new("@pUpdatedBy", session.ProviderId),
            new("@pIsActive", true),
            new("@pIsDeleted", false),
            new("@pCountryCode", DBNull.Value),
            new("@pDialingCode", DBNull.Value),
            new("@pUserLoggingID", -1),
            new("@pSourceName", "Hiso Web Service")
        }, ct);
    }

    private async Task<int> SaveCityAsync(DataTable table, HealthLinkSession session, int countryId, CancellationToken ct)
    {
        var cityName = GetColumnValue(table, "PatientEmployerOrganisation_PhysicalAddress_City");
        if (string.IsNullOrEmpty(cityName))
        {
            return 0;
        }

        var parameters = new List<SqlParameter>
        {
            new("@pCityAreaID", 0),
            new("@pCityAreaTitle", cityName)
        };
        if (countryId > 0)
        {
            parameters.Add(new SqlParameter("@pCountryID", countryId));
        }

        parameters.Add(new SqlParameter("@pUpdatedBy", session.ProviderId));
        parameters.Add(new SqlParameter("@pIsActive", true));
        parameters.Add(new SqlParameter("@pHMLAreaID", DBNull.Value));
        parameters.Add(new SqlParameter("@pIsDeleted", false));
        parameters.Add(new SqlParameter("@pUserLoggingID", -1));
        parameters.Add(new SqlParameter("@pSourceName", "Hiso Web Service"));
        parameters.Add(new SqlParameter("@pCode", DBNull.Value));

        return await RunLookupUpsertAsync(session, "[Lookup].[uspCityAreaInsertUpdate]", parameters, ct);
    }

    private async Task<int> SaveSuburbAsync(DataTable table, HealthLinkSession session, int cityAreaId, CancellationToken ct)
    {
        var suburbName = GetColumnValue(table, "PatientEmployerOrganisation_PhysicalAddress_Suburb");
        if (string.IsNullOrEmpty(suburbName))
        {
            return 0;
        }

        var parameters = new List<SqlParameter>
        {
            new("@pSuburbTownID", 0),
            new("@pSuburbTownTitle", suburbName)
        };
        if (cityAreaId > 0)
        {
            parameters.Add(new SqlParameter("@pCityAreaID", cityAreaId));
        }

        parameters.Add(new SqlParameter("@pHMLTownSuburbID", DBNull.Value));
        parameters.Add(new SqlParameter("@pUpdatedBy", session.ProviderId));
        parameters.Add(new SqlParameter("@pIsActive", true));
        parameters.Add(new SqlParameter("@pIsDeleted", false));
        parameters.Add(new SqlParameter("@pUserLoggingID", -1));
        parameters.Add(new SqlParameter("@pSourceName", "Hiso Web Service"));

        return await RunLookupUpsertAsync(session, "Lookup.uspSuburbTownInsertUpdate", parameters, ct);
    }

    private async Task<int> SaveDesignationAsync(DataTable table, HealthLinkSession session, CancellationToken ct)
    {
        var designationName = GetColumnValue(table, "Patient_Occupation_Description");
        if (string.IsNullOrEmpty(designationName))
        {
            return 0;
        }

        return await RunLookupUpsertAsync(session, "[Lookup].[uspDesignationInsertUpdate]", new List<SqlParameter>
        {
            new("@pDesignationID", 0),
            new("@pDesignationName", designationName),
            new("@pIsActive", true),
            new("@pIsDeleted", false),
            new("@pUpdatedBy", session.ProviderId),
            new("@pUserLoggingID", -1)
        }, ct);
    }

    private async Task<int> RunLookupUpsertAsync(HealthLinkSession session, string procedureName, List<SqlParameter> parameters, CancellationToken ct)
    {
        string connectionString;
        try
        {
            connectionString = await _indiciMasterResolver.ResolveIndiciMasterAsync(ct);
        }
        catch (NotFoundException)
        {
            return 0;
        }

        var outParam = new SqlParameter("@pOutPutParam", SqlDbType.Int) { Direction = ParameterDirection.Output };
        parameters.Add(outParam);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, procedureName, parameters, ct);
        return outParam.Value is DBNull or null ? 0 : Convert.ToInt32(outParam.Value);
    }

    private static string GetColumnValue(DataTable table, string columnName)
    {
        if (table.Rows.Count == 0 || !table.Columns.Contains(columnName))
        {
            return string.Empty;
        }

        var value = table.Rows[0][columnName];
        return value is DBNull ? string.Empty : value?.ToString() ?? string.Empty;
    }

    private static string? ResolveReferenceIdOrFallback(HealthLinkSession session, string fallback) =>
        session.ReferenceId is { Length: > 0 } refId ? refId : fallback;

    private async Task<DataTable> BuildSectionFieldTableAsync(string udtSecretKey, string submittedDataXml, DataTable mapping, string? referenceId, CancellationToken ct)
    {
        var columns = await GetColumnsAsync(udtSecretKey, ct);
        var table = new DataTable();
        foreach (var column in columns)
        {
            table.Columns.Add(column);
        }

        var row = table.NewRow();
        foreach (DataColumn column in table.Columns)
        {
            row[column] = DBNull.Value;
        }

        if (table.Columns.Contains("ReferenceId"))
        {
            row["ReferenceId"] = referenceId ?? (object)0;
        }

        var doc = new XmlDocument();
        doc.LoadXml(submittedDataXml);
        // Real submittedData's `<form>` payload carries the `urn:net.healthlink.genericform.model`
        // namespace, and once it round-trips through SoapCore's `[XmlAnyElement]` handling .NET
        // re-serializes it with an explicit `urn:` prefix (confirmed live 2026-07-30, same root cause as
        // `HisoRequestEngine.ParseRequest`'s equivalent fix) - plain `SelectNodes("//section")` XPath only
        // matches no-namespace elements, and `Name != "field"` only matches the exact unprefixed name;
        // both silently miss every real request. `GetElementsByTagName(name, "*")`/`LocalName` match
        // regardless of prefix/namespace.
        var sections = doc.DocumentElement?.GetElementsByTagName("section", "*");
        if (sections is not null)
        {
            foreach (XmlNode section in sections)
            {
                foreach (XmlNode field in section.ChildNodes)
                {
                    if (field.LocalName != "field")
                    {
                        continue;
                    }

                    var attrName = field.Attributes?["conceptName"]?.Value;
                    if (string.IsNullOrEmpty(attrName))
                    {
                        attrName = field.Attributes?["name"]?.Value;
                    }

                    if (string.IsNullOrEmpty(attrName))
                    {
                        continue;
                    }

                    var columnName = ResolveColumnName(mapping, attrName);
                    if (columnName is not null && table.Columns.Contains(columnName))
                    {
                        row[columnName] = field.InnerText;
                    }
                }
            }
        }

        table.Rows.Add(row);
        return table;
    }

    private async Task<string[]> GetColumnsAsync(string udtSecretKey, CancellationToken ct)
    {
        var raw = await _secretProvider.GetSecretAsync(udtSecretKey, ct);
        return (raw ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static async Task<DataTable> GetConceptMappingTableAsync(string connectionString, CancellationToken ct) =>
        await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[Appointment].[uspGetHL7ConceptMapping]", ct: ct);

    private static string? ResolveColumnName(DataTable mapping, string conceptName)
    {
        var match = mapping.Select($"ConceptName='{conceptName.Replace("'", "''")}' OR Description='{conceptName.Replace("'", "''")}'");
        return match.Length > 0 ? match[0]["ColumnName"]?.ToString() : null;
    }

    private static List<SqlParameter> BuildStandardParameters(string tvpParamName, DataTable table, HealthLinkSession session) => new()
    {
        new SqlParameter(tvpParamName, table),
        new SqlParameter("@pPatientId", session.PatientId),
        new SqlParameter("@pAppointmentId", session.AppointmentId),
        new SqlParameter("@pPracticeId", session.PracticeId),
        new SqlParameter("@pUpdatedBy", session.ProviderId),
        new SqlParameter("@pUserLoggingID", -1)
    };

    private static async Task ExecuteWithOutParamAsync(string connectionString, string procedureName, List<SqlParameter> parameters, string failureMessage, CancellationToken ct)
    {
        var outParam = new SqlParameter("@pOutPutParam", SqlDbType.Int) { Direction = ParameterDirection.Output };
        parameters.Add(outParam);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, procedureName, parameters, ct);
        if (outParam.Value is not (DBNull or null) && Convert.ToInt32(outParam.Value) < 0)
        {
            throw new InvalidOperationException(failureMessage);
        }
    }
}
