using HomeApps.BuildingBlocks.Testing;
using ServiceName.Infrastructure.Persistence;

namespace ServiceName.IntegrationTests;

public sealed class ServiceNameApiFactory : HomeAppsApiFactory<Program, ServiceNameDbContext>;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ServiceNameApiFactory>
{
    public const string Name = "ServiceName API";
}
