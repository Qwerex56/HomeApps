namespace HomeMix.Api.Tests;

/// <summary>Release 3.0 (2.1): an empty HOMEMIX_CLIENT_SECRET stops the start, before the migrations run.</summary>
public sealed class StartupSecretTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    [Fact]
    public void An_empty_client_secret_stops_the_start_with_its_name()
    {
        using var broken = api.WithWebHostBuilder(builder => builder.UseSetting("HomeMix:ClientSecret", string.Empty));

        var error = Assert.ThrowsAny<Exception>(() => broken.CreateClient());

        Assert.Contains("HOMEMIX_CLIENT_SECRET", error.ToString(), StringComparison.Ordinal);
    }
}
