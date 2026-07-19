using HekCoreApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HekCoreApi.Infrastructure.Persistence.Configurations;

public sealed class PracticeRegistryEntryConfiguration : IEntityTypeConfiguration<PracticeRegistryEntry>
{
    public void Configure(EntityTypeBuilder<PracticeRegistryEntry> builder)
    {
        builder.ToTable("Practices");
        builder.HasKey(p => p.PracticeId);

        builder.Property(p => p.PracticeId).HasMaxLength(64);
        builder.Property(p => p.PracticeName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.SourceSystem).HasMaxLength(16).IsRequired();
        builder.Property(p => p.DbServerHost).HasMaxLength(256).IsRequired();
        builder.Property(p => p.DbName).HasMaxLength(128).IsRequired();
    }
}
