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

    public Task<PracticeRoute?> ResolveRouteAsync(string practiceId, CancellationToken ct = default) =>
        ResolveRouteAsync(RoutingContext.FromPracticeId(practiceId, default), ct);

    public async Task<PracticeRoute?> ResolveRouteAsync(RoutingContext context, CancellationToken ct = default)
    {
        var query = _db.Practices.AsNoTracking().Where(p =>
            p.PracticeId == context.PracticeId &&
            p.PracticeCode == context.PracticeCode &&
            p.Environment == context.Environment &&
            p.IsActive);

        // A real composite context (PracticeCode/Environment supplied, not the flat-PracticeId
        // sentinel) also matches on SourceSystem - the same triple can legitimately have separate
        // Karo/Erms rows (e.g. practice 901/environment "local"), discovered live 2026-07-22. The
        // flat-PracticeId overload (used by the other 29 repositories, which don't know their origin)
        // deliberately ignores SourceSystem, matching existing single-row-per-practiceId behaviour.
        if (context.PracticeCode != RoutingContext.Unscoped || context.Environment != RoutingContext.Unscoped)
        {
            query = query.Where(p => p.SourceSystem == context.SourceSystem.ToString());
        }

        var entry = await query.SingleOrDefaultAsync(ct);

        return entry is null
            ? null
            : new PracticeRoute(entry.PracticeId, entry.SourceSystem, entry.DbServerHost, entry.DbName, entry.RowLevelSecurityEnabled);
    }
}
