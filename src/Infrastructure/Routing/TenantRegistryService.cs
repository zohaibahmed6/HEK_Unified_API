using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HekCoreApi.Infrastructure.Routing;

public sealed class TenantRegistryService : ITenantRegistryService
{
    private readonly TenantRegistryDbContext _db;

    public TenantRegistryService(TenantRegistryDbContext db)
    {
        _db = db;
    }

    public async Task<PracticeRoute?> ResolveRouteAsync(string practiceId, CancellationToken ct = default)
    {
        var entry = await _db.Practices
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.PracticeId == practiceId && p.IsActive, ct);

        return entry is null
            ? null
            : new PracticeRoute(entry.PracticeId, entry.SourceSystem, entry.DbServerHost, entry.DbName, entry.RowLevelSecurityEnabled);
    }
}
