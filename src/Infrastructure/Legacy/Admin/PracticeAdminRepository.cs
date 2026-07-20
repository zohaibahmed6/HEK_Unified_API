using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Admin;
using HekCoreApi.Domain.Entities;
using HekCoreApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HekCoreApi.Infrastructure.Legacy.Admin;

/// <summary>
/// Writes onto Block 1's tenant registry (<see cref="TenantRegistryDbContext"/>), not a legacy
/// practice database - this is platform-owned data, not something routed via
/// ILegacyPracticeConnectionResolver.
///
/// FLAGGED RECONCILIATION: the OpenAPI PracticeInput contract has one field
/// (`databaseServerId`), while Block 1's `PracticeRegistryEntry` entity (an inferred design, see
/// ADR-012 Decision 7) has separate `DbServerHost`/`DbName`/`SourceSystem` columns. Mapped here as
/// `databaseServerId` -> `DbServerHost`, with `DbName` defaulted to the same value and
/// `SourceSystem` defaulted to "Unknown" (the contract has no field to source it from) - flagged
/// for reconciliation once the registry schema is confirmed against a real design, not presented
/// as settled.
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
        var now = DateTimeOffset.UtcNow;

        _db.Practices.Add(new PracticeRegistryEntry
        {
            PracticeId = practiceId,
            PracticeName = input.Name,
            SourceSystem = "Unknown",
            DbServerHost = input.DatabaseServerId,
            DbName = input.DatabaseServerId,
            RowLevelSecurityEnabled = false,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
        return Practice.FromInput(practiceId, input);
    }

    public async Task<Practice?> GetAsync(string practiceId, CancellationToken ct = default)
    {
        var entry = await _db.Practices.AsNoTracking().SingleOrDefaultAsync(p => p.PracticeId == practiceId && p.IsActive, ct);
        return entry is null ? null : new Practice(entry.PracticeId, entry.PracticeName, null, entry.DbServerHost);
    }

    public async Task<Practice?> UpdateAsync(string practiceId, PracticeInput input, CancellationToken ct = default)
    {
        var entry = await _db.Practices.SingleOrDefaultAsync(p => p.PracticeId == practiceId && p.IsActive, ct);
        if (entry is null)
        {
            return null;
        }

        entry.PracticeName = input.Name;
        entry.DbServerHost = input.DatabaseServerId;
        entry.DbName = input.DatabaseServerId;
        entry.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Practice.FromInput(practiceId, input);
    }
}
