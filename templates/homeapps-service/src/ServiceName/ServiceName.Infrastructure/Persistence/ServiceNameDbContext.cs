using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
//#if (sample)
using ServiceName.Domain.Echoes;
//#endif

namespace ServiceName.Infrastructure.Persistence;

/// <summary>
/// The service's database and its unit of work. Entity mappings live next to it as IEntityTypeConfiguration classes
/// and are picked up automatically. Add a migration with:
/// <c>dotnet ef migrations add Name -p src/ServiceName/ServiceName.Infrastructure -s src/ServiceName/ServiceName.Api -o Persistence/Migrations</c>
/// </summary>
public sealed class ServiceNameDbContext(DbContextOptions<ServiceNameDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    public const string Schema = "servicelower";

//#if (sample)
    public DbSet<Echo> Echoes => Set<Echo>();

//#endif
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }
}
