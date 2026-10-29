using HomeApps.BuildingBlocks.Testing;
using HomeApps.Plugins.Hello;
using Microsoft.Extensions.DependencyInjection;

namespace HomeApps.Plugins.Tests;

/// <summary>The Hello plugin on a real Postgres, with Households faked at the HTTP level.</summary>
public sealed class HelloPluginFactory : HomeAppsApiFactory<Program, HelloDbContext>
{
    public FakeHouseholds Households { get; } = new();

    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddHttpClient(nameof(IHouseholdAccess)).ConfigurePrimaryHttpMessageHandler(() => Households);
}
