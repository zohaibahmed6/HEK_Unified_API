using HekCoreApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HekCoreApi.Infrastructure.Persistence.Configurations;

public sealed class HisoSessionRegistryEntryConfiguration : IEntityTypeConfiguration<HisoSessionRegistryEntry>
{
    public void Configure(EntityTypeBuilder<HisoSessionRegistryEntry> builder)
    {
        builder.ToTable("HisoSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.PracticeId).HasMaxLength(64).IsRequired();
        builder.Property(s => s.PracticeCode).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Environment).HasMaxLength(64).IsRequired();
        builder.Property(s => s.PracticeName).HasMaxLength(256).IsRequired();
        builder.Property(s => s.SourceSystem).HasMaxLength(16).IsRequired();
        builder.Property(s => s.DbServerHost).HasMaxLength(256).IsRequired();
        builder.Property(s => s.DbName).HasMaxLength(128).IsRequired();

        builder.HasIndex(s => s.SessionGuid).IsUnique();
    }
}
