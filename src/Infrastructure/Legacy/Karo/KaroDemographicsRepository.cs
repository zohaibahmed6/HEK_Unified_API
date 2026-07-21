using System.Data;
using HekCoreApi.Application.Common.Interfaces;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>Ported from `HSSDA.GetHSSDemographics` - real `[HSS].[uspGetDemographics]`, single `@pPatientId` param, 2-table DataSet.</summary>
public sealed class KaroDemographicsRepository : IKaroDemographicsRepository
{
    private readonly IKaroPracticeConnectionResolver _connectionResolver;

    public KaroDemographicsRepository(IKaroPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<KaroDemographicsResult> GetAsync(string practiceSuffix, string? patientId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceSuffix, ct);
        var parameters = new List<Microsoft.Data.SqlClient.SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value) };

        var dataSet = await LegacyDbExecutor.ExecuteDataSetAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDemographics]", parameters, ct);

        KaroDemographicInfo? demographic = null;
        if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
        {
            var row = dataSet.Tables[0].Rows[0];
            demographic = new KaroDemographicInfo(
                Str(row, "Nhi"), Str(row, "BirthDate"), Str(row, "Type"), Str(row, "TitleCode"), Str(row, "Given"), Str(row, "Family"), Str(row, "Gender"),
                Int(row, "Ethnicity1"), Int(row, "Ethnicity2"), Int(row, "Ethnicity3"), Str(row, "Quintile"), Str(row, "Meshblock"), Str(row, "CellNumber"),
                Str(row, "DayPhone"), Str(row, "Email"), Str(row, "FullAddress"), Str(row, "EnrolmentStatus"), Str(row, "SmokingStatus"),
                Str(row, "Street"), Str(row, "Suburb"), Str(row, "City"), Str(row, "PostCode"), Str(row, "IsNZResident"), Str(row, "DateOfEnrolment"));
        }

        var cards = new List<KaroCardInfo>();
        if (dataSet.Tables.Count > 1)
        {
            // Legacy: only indices [0]/[1] are ever inspected, each only included if its own cardtype is non-empty.
            for (var i = 0; i < dataSet.Tables[1].Rows.Count && i < 2; i++)
            {
                var row = dataSet.Tables[1].Rows[i];
                var cardType = Str(row, "cardtype");
                if (!string.IsNullOrEmpty(cardType))
                {
                    cards.Add(new KaroCardInfo(cardType, Str(row, "startdate"), Str(row, "expirydate"), Str(row, "cardnumber")));
                }
            }
        }

        return new KaroDemographicsResult(demographic, cards);
    }

    private static string? Str(DataRow row, string column) =>
        row.Table.Columns.Contains(column) && row[column] is not DBNull ? row[column].ToString() : null;

    private static int Int(DataRow row, string column) =>
        row.Table.Columns.Contains(column) && row[column] is not DBNull && int.TryParse(row[column].ToString(), out var value) ? value : 0;
}
