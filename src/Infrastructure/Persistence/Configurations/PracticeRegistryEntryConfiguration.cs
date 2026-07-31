using HekCoreApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HekCoreApi.Infrastructure.Persistence.Configurations;

public sealed class PracticeRegistryEntryConfiguration : IEntityTypeConfiguration<PracticeRegistryEntry>
{
    public void Configure(EntityTypeBuilder<PracticeRegistryEntry> builder)
    {
        builder.ToTable("Practices");
        builder.HasKey(p => p.Id);

        // PracticeId/PracticeCode/Environment are each nullable - a row only fills whichever field(s)
        // its routing tier actually uses (see TenantRegistryService.ResolveRouteAsync's 3 tiers),
        // rather than padding unused columns with a sentinel placeholder value.
        builder.Property(p => p.PracticeId).HasMaxLength(64);
        builder.Property(p => p.PracticeCode).HasMaxLength(64);
        builder.Property(p => p.Environment).HasMaxLength(64);
        builder.Property(p => p.PracticeName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.SourceSystem).HasMaxLength(16).IsRequired();
        builder.Property(p => p.DbServerHost).HasMaxLength(256).IsRequired();
        builder.Property(p => p.DbName).HasMaxLength(128).IsRequired();

        // SourceSystem is part of the key, not just informational: the same PracticeId/PracticeCode/
        // Environment triple can legitimately have separate KARO and ERMS routing rows (discovered
        // live, 2026-07-22 - inserting both for practice 901/local violated the earlier 3-column index).
        // SQL Server treats NULLs as distinct in a unique index, so multiple rows differing only by
        // which field is null (one row's PracticeCode, another's Environment) don't collide.
        builder.HasIndex(p => new { p.PracticeId, p.PracticeCode, p.Environment, p.SourceSystem }).IsUnique();

        // Per-tier uniqueness guards (2026-07-28, corrected after a live test caught the first attempt
        // being too narrow): each filter mirrors TenantRegistryService.ResolveRouteAsync's actual WHERE
        // clause for that tier exactly, not just "the fields that are usually null" for that tier -
        // Tier 3's query, for example, matches on Environment+SourceSystem ALONE (it never checks
        // PracticeId/PracticeCode on the row at all), so a second row with a *different* PracticeId but
        // the same Environment+SourceSystem is just as ambiguous as an exact duplicate - confirmed live:
        // inserting (PracticeId=NULL, Environment='local') then (PracticeId='999', Environment='local')
        // both for Karo was NOT rejected by an earlier, incorrectly-scoped version of this index, and
        // both rows would have matched a real Tier-3 lookup and crashed SingleOrDefaultAsync.
        builder.HasIndex(p => new { p.Environment, p.SourceSystem })
            .IsUnique()
            .HasFilter("[Environment] IS NOT NULL AND [IsActive] = 1")
            .HasDatabaseName("IX_Practices_Tier3_EnvironmentOnly");

        builder.HasIndex(p => new { p.PracticeId, p.PracticeCode, p.SourceSystem })
            .IsUnique()
            .HasFilter("[PracticeCode] IS NOT NULL AND [IsActive] = 1")
            .HasDatabaseName("IX_Practices_Tier2_PracticeIdAndCode");

        builder.HasIndex(p => new { p.PracticeId, p.SourceSystem })
            .IsUnique()
            .HasFilter("[PracticeCode] IS NULL AND [Environment] IS NULL AND [IsActive] = 1")
            .HasDatabaseName("IX_Practices_Tier1_PracticeIdOnly");
    }
}
