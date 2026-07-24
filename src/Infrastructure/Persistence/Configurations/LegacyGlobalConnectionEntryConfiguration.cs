using HekCoreApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HekCoreApi.Infrastructure.Persistence.Configurations;

public sealed class LegacyGlobalConnectionEntryConfiguration : IEntityTypeConfiguration<LegacyGlobalConnectionEntry>
{
    public void Configure(EntityTypeBuilder<LegacyGlobalConnectionEntry> builder)
    {
        builder.ToTable("LegacyGlobalConnections");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Key).HasMaxLength(64).IsRequired();
        builder.Property(c => c.DbServerHost).HasMaxLength(256).IsRequired();
        builder.Property(c => c.DbName).HasMaxLength(128).IsRequired();
        builder.Property(c => c.CredentialSecretKey).HasMaxLength(256).IsRequired();

        builder.HasIndex(c => c.Key).IsUnique();
    }
}
