using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using HomeMix.Domain.Blocks;
using HomeMix.Domain.Boards;
using HomeMix.Domain.Data;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;
using HomeMix.Domain.Store;
using HomeMix.Domain.Triggers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeMix.Infrastructure.Persistence;

/// <summary>HomeMix's own database: packages (what is in each zip) and installations in households and on accounts, the add-ons' data, the Owner's blocks, the store's entries and its numbers and "domki", where each household's widgets stand, and how each add-on trigger is doing.</summary>
public sealed class HomeMixDbContext(DbContextOptions<HomeMixDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    public DbSet<AddonPackage> Packages => Set<AddonPackage>();

    public DbSet<Installation> Installations => Set<Installation>();

    public DbSet<AddonDocument> Documents => Set<AddonDocument>();

    public DbSet<AddonBlock> Blocks => Set<AddonBlock>();

    public DbSet<StoreListing> StoreListings => Set<StoreListing>();

    public DbSet<StoreEvent> StoreEvents => Set<StoreEvent>();

    public DbSet<StoreHeart> StoreHearts => Set<StoreHeart>();

    public DbSet<WidgetBoard> Boards => Set<WidgetBoard>();

    public DbSet<TriggerState> TriggerStates => Set<TriggerState>();
}
