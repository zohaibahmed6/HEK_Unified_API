using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>Ported from `HSSDA.GetDemographics` (`DAL/South/HSSDA.cs:239`) - real `[HSS].[uspGetDemographics]` on `ConnIndiciDB{practiceid}`.</summary>
public sealed class ErmsDemographicsRepository : IErmsDemographicsRepository
{
    private readonly IErmsPracticeConnectionResolver _connectionResolver;

    public ErmsDemographicsRepository(IErmsPracticeConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<DataTable> GetDemographicsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(routingContext, ct);
        var parameters = new List<SqlParameter> { new("@pPatientId", (object?)patientId ?? DBNull.Value) };

        return await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDemographics]", parameters, ct);
    }
}
