using Devices.Domain.Capabilities;
using Devices.Domain.Devices;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Devices.Infrastructure.Persistence;

/// <summary>Devices' own database: the registry of devices with their actions and descriptors, what they reported (last state, event history) and the namespaced capabilities they brought.</summary>
public sealed class DevicesDbContext(DbContextOptions<DevicesDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    /// <summary>A collation that ignores case (and nothing else), for names that are unique "regardless of case".</summary>
    public const string CaseInsensitive = "case_insensitive";

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<DeviceState> DeviceStates => Set<DeviceState>();

    public DbSet<DeviceEvent> DeviceEvents => Set<DeviceEvent>();

    public DbSet<RegisteredCapability> RegisteredCapabilities => Set<RegisteredCapability>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasCollation(CaseInsensitive, locale: "und-u-ks-level2", provider: "icu", deterministic: false);
    }
}
