using HekCoreApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HekCoreApi.Infrastructure.Persistence;

/// <summary>
/// Local-dev/test-only seed data - one fake practice per SourceSystem, so routing logic
/// (ITenantRegistryService) has real rows to resolve against without a live legacy DB. Invoked
/// conditionally in Development/Testing only (see Program.cs) - never runs in production.
/// </summary>
public static class TenantRegistrySeeder
{
    public static async Task SeedAsync(TenantRegistryDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Practices.AnyAsync(ct))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        db.Practices.AddRange(
            new PracticeRegistryEntry
            {
                PracticeId = "HISO-DEV-001",
                PracticeName = "HISO Dev Practice",
                SourceSystem = "Hiso",
                DbServerHost = "localhost",
                DbName = "HekDev_Hiso",
                RowLevelSecurityEnabled = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new PracticeRegistryEntry
            {
                PracticeId = "KARO-DEV-001",
                PracticeName = "KARO Dev Practice",
                SourceSystem = "Karo",
                DbServerHost = "localhost",
                DbName = "HekDev_Karo",
                RowLevelSecurityEnabled = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new PracticeRegistryEntry
            {
                PracticeId = "ERMS-DEV-001",
                PracticeName = "ERMS Dev Practice",
                SourceSystem = "Erms",
                DbServerHost = "localhost",
                DbName = "HekDev_Erms",
                RowLevelSecurityEnabled = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new PracticeRegistryEntry
            {
                PracticeId = "COL-DEV-001",
                PracticeName = "COL/Pegasus Dev Practice",
                SourceSystem = "Col",
                DbServerHost = "localhost",
                DbName = "HekDev_Erms",
                RowLevelSecurityEnabled = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        await db.SaveChangesAsync(ct);
    }
}
