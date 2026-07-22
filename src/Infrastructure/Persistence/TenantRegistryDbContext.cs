using HekCoreApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HekCoreApi.Infrastructure.Persistence;

/// <summary>
/// New, small, separate database (ADR-001) - never the legacy per-practice Indici PMS schema
/// (ADR-010: no changes to legacy practice schemas). Deliberately its own DbContext so nothing here
/// can accidentally touch a legacy schema.
/// </summary>
public sealed class TenantRegistryDbContext : DbContext
{
    public TenantRegistryDbContext(DbContextOptions<TenantRegistryDbContext> options) : base(options)
    {
    }

    public DbSet<PracticeRegistryEntry> Practices => Set<PracticeRegistryEntry>();

    public DbSet<HisoSessionRegistryEntry> HisoSessions => Set<HisoSessionRegistryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantRegistryDbContext).Assembly);
    }
}
