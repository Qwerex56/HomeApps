using Devices.Domain.Devices;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Devices.Infrastructure.Persistence;

/// <summary>Devices' own database: the registry of devices with their actions.</summary>
public sealed class DevicesDbContext(DbContextOptions<DevicesDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    public DbSet<Device> Devices => Set<Device>();
}
