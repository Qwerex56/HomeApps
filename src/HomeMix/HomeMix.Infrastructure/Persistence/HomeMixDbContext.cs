using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using HomeMix.Domain.Blocks;
using HomeMix.Domain.Data;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

/// <summary>HomeMix's own database: packages (what is in each zip) and installations in households and on accounts, the add-ons' data and the Owner's blocks.</summary>
public sealed class HomeMixDbContext(DbContextOptions<HomeMixDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    public DbSet<AddonPackage> Packages => Set<AddonPackage>();

    public DbSet<Installation> Installations => Set<Installation>();

    public DbSet<AddonDocument> Documents => Set<AddonDocument>();

    public DbSet<AddonBlock> Blocks => Set<AddonBlock>();
}
