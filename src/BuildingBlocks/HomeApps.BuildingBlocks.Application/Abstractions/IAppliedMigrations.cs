namespace HomeApps.BuildingBlocks.Application.Abstractions;

/// <summary>The EF Core migrations applied to the service's database, for <c>GET /health/version</c>.</summary>
public interface IAppliedMigrations
{
    Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken);
}
