using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeApps.Plugins.Hosting;

/// <summary>
/// Base DbContext for a plugin: <see cref="HomeAppsDbContext"/> (unit of work, domain events, outbox) plus the
/// plugin's <see cref="PluginEnablement"/> table. Migrations of the derived context include that table.
/// </summary>
public abstract class PluginDbContext(DbContextOptions options, IPublisher publisher) : HomeAppsDbContext(options, publisher)
{
    public DbSet<PluginEnablement> PluginEnablements => Set<PluginEnablement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new PluginEnablementConfiguration());
    }

    private sealed class PluginEnablementConfiguration : IEntityTypeConfiguration<PluginEnablement>
    {
        public void Configure(EntityTypeBuilder<PluginEnablement> builder)
        {
            builder.ToTable("PluginEnablements");
            builder.HasKey(e => e.HouseholdId);
            builder.Property(e => e.HouseholdId).ValueGeneratedNever();
            builder.Property<uint>("Version").IsRowVersion();
            builder.Ignore(e => e.DomainEvents);
        }
    }
}
