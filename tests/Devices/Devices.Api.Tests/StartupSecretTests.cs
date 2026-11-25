using Microsoft.AspNetCore.Hosting;

namespace Devices.Api.Tests;

/// <summary>Release 3.0 (2.1): an empty DEVICES_CLIENT_SECRET stops the start, before the migrations run.</summary>
public sealed class StartupSecretTests
{
    [Fact]
    public async Task An_empty_client_secret_stops_the_start_with_its_name()
    {
        await using var api = new EmptySecretFactory();

        var error = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await api.InitializeAsync();
            _ = api.CreateClient();
        });

        Assert.Contains("DEVICES_CLIENT_SECRET", error.ToString(), StringComparison.Ordinal);
    }

    private sealed class EmptySecretFactory : DevicesApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Devices:ClientSecret", string.Empty);
        }
    }
}
