using System.Net.Http.Headers;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace HomeApps.BuildingBlocks.Testing;

/// <summary>
/// Runs a service's API in memory against a real Postgres (Testcontainers, needs Docker) with MassTransit's in-memory
/// test harness instead of RabbitMQ, and JWT validation switched to <see cref="TestJwt"/>.
/// Use it as an xUnit class or collection fixture.
/// </summary>
public class HomeAppsApiFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
    where TDbContext : HomeAppsDbContext
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>The bus harness: assert on published and consumed messages.</summary>
    public ITestHarness Harness => Services.GetTestHarness();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Starts the host; the schema initializer runs before the bus and outbox services.
        _ = Services;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>A client that sends a valid access token for <paramref name="userId"/>.</summary>
    public HttpClient CreateAuthenticatedClient(Guid userId, string? email = null, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId, email, roles));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Authority", TestJwt.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwt.Audience);
        builder.UseSetting(HomeApps.BuildingBlocks.Infrastructure.DependencyInjection.MessagingTransportKey,
            HomeApps.BuildingBlocks.Infrastructure.DependencyInjection.InMemoryTransport);

        builder.ConfigureTestServices(services =>
        {
            // First hosted service, so the outbox and inbox services never see a database without tables.
            services.Insert(0, ServiceDescriptor.Singleton<IHostedService, SchemaInitializer<TDbContext>>());

            // The outbox delivers after commit, so give the harness time before it decides the bus went quiet.
            services.Configure<TestHarnessOptions>(options =>
            {
                options.TestTimeout = TimeSpan.FromSeconds(30);
                options.TestInactivityTimeout = TimeSpan.FromSeconds(10);
            });

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.MetadataAddress = null!;
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = TestJwt.Issuer,
                };
                options.Configuration.SigningKeys.Add(TestJwt.SigningKey);
                options.TokenValidationParameters.IssuerSigningKey = TestJwt.SigningKey;
            });

            ConfigureServices(services);
        });
    }

    /// <summary>Override to replace more services for a specific service's tests.</summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
