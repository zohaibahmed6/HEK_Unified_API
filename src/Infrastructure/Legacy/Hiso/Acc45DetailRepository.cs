using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Ported from Acc45Builder.cs/Acc45DiagnosisBuilder.cs/Acc45ReferralBuilder.cs/Mapper.cs.
/// Field-to-column resolution via the real `Appointment.uspGetHL7ConceptMapping` table (matched by
/// ConceptName-or-Description), with the 3 confirmed real business rules applied where relevant.
/// </summary>
public sealed class Acc45DetailRepository : IAcc45DetailRepository
{
    private readonly IHisoPracticeConnectionResolver _connectionResolver;
    private readonly ISecretProvider _secretProvider;

    public Acc45DetailRepository(IHisoPracticeConnectionResolver connectionResolver, ISecretProvider secretProvider)
    {
        _connectionResolver = connectionResolver;
        _secretProvider = secretProvider;
    }

    public async Task SaveAccidentInformationAsync(string submittedDataXml, HealthLinkSession session, string formEngineId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var mapping = await GetConceptMappingTableAsync(connectionString, ct);

        var doc = new XmlDocument();
        doc.LoadXml(submittedDataXml);

        var detailColumns = await GetColumnsAsync("UDT_tblACC45Detail", ct);
        var detailTable = BuildDetailTable(doc, mapping, session, detailColumns);
        var diagnosisTable = BuildDiagnosisTable(doc, mapping, session);
        var referralTable = BuildReferralTable(doc, mapping, session);

        var parameters = new List<SqlParameter>
        {
            new("@tblACC45Detail", detailTable),
            new("@tblAcc45Diagnosis", diagnosisTable),
            new("@FormInstanceVersion", "1"),
            new("@FormEngineID", formEngineId),
            new("@Mode", 1),
            new("@tblAcc45Referral", referralTable)
        };

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[Appointment].[usptblACC45Detail_InsertUpdate_New]", parameters, ct);
    }

    private static async Task<DataTable> GetConceptMappingTableAsync(string connectionString, CancellationToken ct) =>
        await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[Appointment].[uspGetHL7ConceptMapping]", ct: ct);

    private async Task<string[]> GetColumnsAsync(string udtSecretKey, CancellationToken ct)
    {
        var raw = await _secretProvider.GetSecretAsync(udtSecretKey, ct);
        return (raw ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? ResolveColumnName(DataTable mapping, string conceptName)
    {
        var match = mapping.Select($"ConceptName='{conceptName.Replace("'", "''")}' OR Description='{conceptName.Replace("'", "''")}'");
        return match.Length > 0 ? match[0]["ColumnName"]?.ToString() : null;
    }

    private static bool IsNumeric(string? value) => decimal.TryParse(value, out _);

    /// <summary>Legacy: Acc45Builder.GenerateTable - general ACC45 detail fields, with the confirmed `employmentStatus` splitting rule.</summary>
    private static DataTable BuildDetailTable(XmlDocument doc, DataTable mapping, HealthLinkSession session, string[] realColumns)
    {
        var table = new DataTable();
        foreach (var column in realColumns)
        {
            table.Columns.Add(column);
        }

        var row = table.NewRow();
        foreach (DataColumn column in table.Columns)
        {
            row[column] = DBNull.Value;
        }

        if (table.Columns.Contains("PatientID"))
        {
            row["PatientID"] = session.PatientId;
        }

        if (table.Columns.Contains("AppointmentID"))
        {
            row["AppointmentID"] = session.AppointmentId;
        }

        if (table.Columns.Contains("PracticeID"))
        {
            row["PracticeID"] = session.PracticeId;
        }

        var fields = doc.DocumentElement?.SelectNodes("//field") ?? doc.SelectNodes("//field");
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
                if (columnName is null || !table.Columns.Contains(columnName))
                {
                    continue;
                }

                var value = field.InnerText;
                if (string.Equals(columnName, "employmentstatus", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsNumeric(value))
                    {
                        row[columnName] = value;
                    }
                    else if (value.Contains(','))
                    {
                        row[columnName] = int.Parse(value.Split(',')[0]);
                    }
                    else if (value.Contains(';'))
                    {
                        row[columnName] = int.Parse(value.Split(';')[0]);
                    }
                }
                else
                {
                    row[columnName] = value;
                }
            }
        }

        table.Rows.Add(row);
        return table;
    }

    /// <summary>
    /// Legacy `Acc45DiagnosisBuilder.GenerateTable` - real, fixed 15-column `UDT_tblACC45Diagnosis`
    /// shape (hardcoded in legacy source as `ACC45DiadColumns`, not a live-schema guess). A group
    /// matches on either `name="accident.diagnosis"` or `conceptName="Patient_Accident_Diagnosis"`
    /// (legacy checks both). Field values resolve through the same concept-mapping table as the
    /// detail table; "side" is comma-split into SideCode/SideDescription (confirmed rule). Defaults
    /// (Acc45ID/Acc45DiagnosisID=0, IsActive=true, IsDeleted=false, Inserted/UpdatedBy=session
    /// ProviderId, Inserted/UpdatedAt=now) match legacy's per-row seeding exactly.
    /// </summary>
    private static readonly string[] DiagnosisColumns =
    [
        "Acc45DiagnosisID", "ACC45ID", "DiagnosisDescription", "CodeSystem", "Code", "SideCode",
        "SideDescription", "QualifierCode", "QualifierDescription", "IsActive", "IsDeleted",
        "InsertedBy", "UpdatedBy", "InsertedAt", "UpdatedAt"
    ];

    /// <summary>Legacy `Acc45ReferralBuilder.GenerateTable` - real, fixed 3-column `UDT_tblACC45Referral` shape.</summary>
    private static readonly string[] ReferralColumns = ["ACCRefID", "Notes", "UpdatedBy"];

    private static DataTable BuildDiagnosisTable(XmlDocument doc, DataTable mapping, HealthLinkSession session)
    {
        var table = new DataTable();
        foreach (var column in DiagnosisColumns)
        {
            table.Columns.Add(column);
        }

        var groups = doc.DocumentElement?.SelectNodes("//group[@name='accident.diagnosis' or @conceptName='Patient_Accident_Diagnosis']");
        if (groups is null || groups.Count == 0)
        {
            return table;
        }

        foreach (XmlNode group in groups)
        {
            if (group.ChildNodes.Count == 0)
            {
                continue;
            }

            var row = table.NewRow();
            row["ACC45ID"] = 0;
            row["Acc45DiagnosisID"] = 0;
            row["IsActive"] = true;
            row["IsDeleted"] = false;
            row["InsertedBy"] = session.ProviderId;
            row["UpdatedBy"] = session.ProviderId;
            row["InsertedAt"] = DateTime.Now.ToString("s");
            row["UpdatedAt"] = DateTime.Now.ToString("s");

            foreach (XmlNode field in group.ChildNodes)
            {
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
                if (columnName is null || !table.Columns.Contains(columnName))
                {
                    continue;
                }

                var value = field.InnerText;
                if (attrName == "side" && !IsNumeric(value))
                {
                    var parts = value.Split(',');
                    row[columnName] = parts[0];
                    row["SideDescription"] = parts.Length > 1 ? parts[1] : string.Empty;
                }
                else
                {
                    row[columnName] = value;
                }
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private static DataTable BuildReferralTable(XmlDocument doc, DataTable mapping, HealthLinkSession session)
    {
        var table = new DataTable();
        foreach (var column in ReferralColumns)
        {
            table.Columns.Add(column);
        }

        var groups = doc.DocumentElement?.SelectNodes("//group[@name='accident.referral']");
        if (groups is null || groups.Count == 0)
        {
            return table;
        }

        foreach (XmlNode group in groups)
        {
            if (group.ChildNodes.Count == 0)
            {
                continue;
            }

            var row = table.NewRow();
            row["UpdatedBy"] = session.ProviderId;

            foreach (XmlNode field in group.ChildNodes)
            {
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
                if (columnName is null || !table.Columns.Contains(columnName))
                {
                    continue;
                }

                var value = field.InnerText;
                row[columnName] = attrName == "treatmentProvider" && !IsNumeric(value)
                    ? value.Split(',')[0].Trim()
                    : value;
            }

            table.Rows.Add(row);
        }

        return table;
    }
}
