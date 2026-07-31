using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Domain.Entities;
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
        // Single-tier match, no fallback cascade - mirrors the real legacy precedence (KaroRoutingResolver/
        // ErmsRoutingResolver already collapse the caller's encounterId down to exactly one of these three
        // shapes before this runs, per the real `ConnIndiciDB{practiceid}` overwrite quirk in HSSDA.cs:813):
        //   1. All three segments present -> route by Environment alone (legacy's real overwrite behavior).
        //   2. PracticeId + PracticeCode present, no environment -> route by both together.
        //   3. PracticeId alone -> route by PracticeId (also the path every flat-PracticeId caller uses -
        //      HISO/COL/the other Block 2 repositories - so SourceSystem is deliberately NOT filtered here,
        //      matching their existing single-row-per-practiceId behaviour).
        // No fallback between tiers: if the tier-appropriate row isn't registered, this returns null and
        // the caller surfaces "not registered" - it never silently tries a looser match.
        IQueryable<PracticeRegistryEntry> query;

        if (context.Environment != RoutingContext.Unscoped)
        {
            query = _db.Practices.AsNoTracking().Where(p =>
                p.Environment == context.Environment && p.SourceSystem == context.SourceSystem.ToString());
        }
        else if (context.PracticeCode != RoutingContext.Unscoped)
        {
            query = _db.Practices.AsNoTracking().Where(p =>
                p.PracticeId == context.PracticeId && p.PracticeCode == context.PracticeCode && p.SourceSystem == context.SourceSystem.ToString());
        }
        else
        {
            // Also require PracticeCode/Environment to be unset on the row - otherwise a bare
            // PracticeId lookup would incidentally match a more specific Tier-2/Tier-3 row for the
            // same PracticeId (e.g. Karo practice 901's PracticeCode-scoped row) and silently return
            // the wrong target. SourceSystem is filtered here too, since Karo/Erms's own dedicated
            // resolvers always carry a real SourceSystem for this tier - the only caller that doesn't
            // (the flat, source-agnostic `RoutingContext.FromPracticeId(id, default)` path used by
            // HISO/COL and the generic Block 2 repositories) relies on its own registry rows already
            // being tagged with a matching SourceSystem.
            query = _db.Practices.AsNoTracking().Where(p =>
                p.PracticeId == context.PracticeId && p.PracticeCode == null && p.Environment == null && p.SourceSystem == context.SourceSystem.ToString());
        }

        var entry = await query.Where(p => p.IsActive).SingleOrDefaultAsync(ct);

        return entry is null
            ? null
            : new PracticeRoute(entry.PracticeId ?? context.PracticeId, entry.SourceSystem, entry.DbServerHost, entry.DbName, entry.RowLevelSecurityEnabled);
    }
}
