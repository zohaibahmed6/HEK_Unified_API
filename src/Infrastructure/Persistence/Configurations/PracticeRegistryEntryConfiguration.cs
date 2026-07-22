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

        builder.Property(p => p.PracticeId).HasMaxLength(64).IsRequired();
        builder.Property(p => p.PracticeCode).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Environment).HasMaxLength(64).IsRequired();
        builder.Property(p => p.PracticeName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.SourceSystem).HasMaxLength(16).IsRequired();
        builder.Property(p => p.DbServerHost).HasMaxLength(256).IsRequired();
        builder.Property(p => p.DbName).HasMaxLength(128).IsRequired();

        // SourceSystem is part of the key, not just informational: the same PracticeId/PracticeCode/
        // Environment triple can legitimately have separate KARO and ERMS routing rows (discovered
        // live, 2026-07-22 - inserting both for practice 901/local violated the earlier 3-column index).
        builder.HasIndex(p => new { p.PracticeId, p.PracticeCode, p.Environment, p.SourceSystem }).IsUnique();
    }
}
