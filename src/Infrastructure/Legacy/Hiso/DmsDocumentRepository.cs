using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Real implementation of <see cref="IDmsProxyService"/> - see that interface's doc comment for why
/// this reads the real `DMS_PMS` database directly (`dbo.uspDocumentGetByDMSID`) instead of calling
/// legacy's external DMSProxy ASMX service, confirmed unreachable even from the real legacy server in
/// this environment. Reuses <see cref="IHisoDmsConnectionResolver"/> - the same per-practice DMS
/// connection HISO's other document work already resolves - rather than duplicating its
/// credential-lookup logic.
/// </summary>
public sealed class DmsDocumentRepository : IDmsProxyService
{
    private readonly IHisoDmsConnectionResolver _dmsConnectionResolver;

    public DmsDocumentRepository(IHisoDmsConnectionResolver dmsConnectionResolver)
    {
        _dmsConnectionResolver = dmsConnectionResolver;
    }

    public async Task<byte[]?> GetDocumentDataAsync(Guid documentGuid, string practiceId, CancellationToken ct = default)
    {
        var connectionString = await _dmsConnectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pDocumentKey", documentGuid),
            new("@pPracticeID", int.Parse(practiceId)),
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "uspDocumentGetByDMSID", parameters, ct);
        if (table.Rows.Count == 0 || table.Rows[0]["DocumentData"] is DBNull)
        {
            return null;
        }

        return (byte[])table.Rows[0]["DocumentData"];
    }
}
