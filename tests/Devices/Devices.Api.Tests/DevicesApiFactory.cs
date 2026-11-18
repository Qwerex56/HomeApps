using Devices.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Testing;
using Microsoft.AspNetCore.Hosting;

namespace Devices.Api.Tests;

/// <summary>The real Devices Api on Postgres; app tokens of JARVIS's <see cref="Resource"/> are accepted like in production.</summary>
public sealed class DevicesApiFactory : HomeAppsApiFactory<Program, DevicesDbContext>
{
    public const string Resource = "https://homeapps.test/jarvis/mcp";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.ConfigureWebHost(builder);
        builder.UseSetting("Devices:Resource", Resource);
        builder.UseSetting("Devices:ActionTimeout", "00:00:03");
    }
}
