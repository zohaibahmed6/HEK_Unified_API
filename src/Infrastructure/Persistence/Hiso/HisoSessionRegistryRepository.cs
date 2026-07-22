using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace HekCoreApi.Infrastructure.Persistence.Hiso;

/// <inheritdoc cref="IHisoSessionRegistryRepository"/>
public sealed class HisoSessionRegistryRepository : IHisoSessionRegistryRepository
{
    private readonly TenantRegistryDbContext _db;

    public HisoSessionRegistryRepository(TenantRegistryDbContext db)
    {
        _db = db;
    }

    public async Task<HisoSessionRoute?> FindAsync(Guid sessionGuid, CancellationToken ct = default)
    {
        var entry = await _db.HisoSessions.AsNoTracking().SingleOrDefaultAsync(s => s.SessionGuid == sessionGuid && s.IsActive, ct);
        return entry is null ? null : new HisoSessionRoute(entry.PracticeId, entry.Environment, entry.DbServerHost, entry.DbName, entry.CreatedAt);
    }

    public async Task<HisoSessionRoute?> FindByPracticeIdAsync(string practiceId, CancellationToken ct = default)
    {
        var entry = await _db.HisoSessions.AsNoTracking()
            .Where(s => s.PracticeId == practiceId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return entry is null ? null : new HisoSessionRoute(entry.PracticeId, entry.Environment, entry.DbServerHost, entry.DbName, entry.CreatedAt);
    }
}
