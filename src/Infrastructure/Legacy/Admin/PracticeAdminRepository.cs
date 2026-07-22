using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Admin;
using HekCoreApi.Contracts.Security;
using HekCoreApi.Domain.Entities;
using HekCoreApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HekCoreApi.Infrastructure.Legacy.Admin;

/// <summary>
/// Writes onto Block 1's tenant registry (<see cref="TenantRegistryDbContext"/>), not a legacy
/// practice database - this is platform-owned data, not something routed via
/// ILegacyPracticeConnectionResolver.
///
/// PROJECT_STATUS.md open items 24/31, resolved 2026-07-20: <see cref="PracticeInput"/> now carries
/// the three real fields <see cref="PracticeRegistryEntry"/> needs (`DbServerHost`/`DbName`/
/// `SourceSystem`) directly, one-to-one, instead of collapsing them into one ambiguous field. The
/// earlier version of this mapping duplicated a single `databaseServerId` value into both
/// `DbServerHost` and `DbName` - a real bug, not just an inference gap, since a practice's database
/// name is never actually equal to its server hostname.
/// </summary>
public sealed class PracticeAdminRepository : IPracticeAdminRepository
{
    private readonly TenantRegistryDbContext _db;

    public PracticeAdminRepository(TenantRegistryDbContext db)
    {
        _db = db;
    }

    public async Task<Practice> RegisterAsync(PracticeInput input, CancellationToken ct = default)
    {
        var practiceId = Guid.NewGuid().ToString("N")[..12];
        var now = DateTimeOffset.Now;

        _db.Practices.Add(new PracticeRegistryEntry
        {
            PracticeId = practiceId,
            PracticeCode = Application.Common.Models.RoutingContext.Unscoped,
            Environment = Application.Common.Models.RoutingContext.Unscoped,
            PracticeName = input.Name,
            SourceSystem = input.SourceSystem.ToString(),
            DbServerHost = input.DbServerHost,
            DbName = input.DbName,
            RowLevelSecurityEnabled = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _db.SaveChangesAsync(ct);
        return Practice.FromInput(practiceId, input);
    }

    public async Task<Practice?> GetAsync(string practiceId, CancellationToken ct = default)
    {
        var entry = await _db.Practices.AsNoTracking().SingleOrDefaultAsync(p => p.PracticeId == practiceId && p.IsActive, ct);
        return entry is null ? null : new Practice(entry.PracticeId, entry.PracticeName, null, entry.DbServerHost, entry.DbName, Enum.Parse<OriginScope>(entry.SourceSystem));
    }

    public async Task<Practice?> UpdateAsync(string practiceId, PracticeInput input, CancellationToken ct = default)
    {
        var entry = await _db.Practices.SingleOrDefaultAsync(p => p.PracticeId == practiceId && p.IsActive, ct);
        if (entry is null)
        {
            return null;
        }

        entry.PracticeName = input.Name;
        entry.SourceSystem = input.SourceSystem.ToString();
        entry.DbServerHost = input.DbServerHost;
        entry.DbName = input.DbName;
        entry.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Practice.FromInput(practiceId, input);
    }
}
