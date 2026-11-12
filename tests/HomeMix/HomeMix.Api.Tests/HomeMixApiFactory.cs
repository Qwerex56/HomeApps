using HomeMix.Api.Mcp;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using HomeMix.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HomeMix.Api.Tests;

/// <summary>
/// The real HomeMix Api on its own Postgres database (<c>HOMEMIX_TEST_DATABASE</c>, otherwise Testcontainers), packages
/// in a temporary folder, and <see cref="FakeHouseholds"/> and <see cref="FakeGitHubReleases"/> instead of the
/// Households service and GitHub. The instance has the plugin <c>todo</c>; data limits are small: 4 KB a
/// document, 64 KB an installation, 3 documents a collection.
/// </summary>
public sealed class HomeMixApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://auth.homeapps.test";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test" };
    private readonly string _packages = Path.Combine(Path.GetTempPath(), $"homemix-tests-{Guid.NewGuid():N}");
    private readonly string _official = Path.Combine(Path.GetTempPath(), $"homemix-official-{Guid.NewGuid():N}");
    private PostgreSqlContainer? _container;
    private string _server = string.Empty;
    private string _database = string.Empty;

    public FakeHouseholds Households { get; } = new();

    public FakeGitHubReleases GitHub { get; } = new();

    public FakeStoreIndex Store { get; } = new();

    public FakeWeb Web { get; } = new();

    /// <summary>The one official add-on of the test instance, as <c>homemix/official</c> would ship it.</summary>
    public static byte[] OfficialWeather { get; } = AddonZip.Create(AddonZip.Manifest(id: "weather", version: "1.0.0",
        capabilities: """["storage.household", "notify"]""", network: """["https://api.open-meteo.com"]"""));

    public async Task InitializeAsync()
    {
        _server = Environment.GetEnvironmentVariable("HOMEMIX_TEST_DATABASE") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_server))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
            _server = _container.GetConnectionString();
        }

        Directory.CreateDirectory(_official);
        await File.WriteAllBytesAsync(Path.Combine(_official, "weather.zip"), OfficialWeather);
        await File.WriteAllTextAsync(Path.Combine(_official, "broken.zip"), "not a zip");

        _database = $"homemix_api_tests_{Guid.NewGuid():N}";
        await using var db = new HomeMixDbContext(
            new DbContextOptionsBuilder<HomeMixDbContext>().UseNpgsql(ConnectionString).Options, new NoPublisher());
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using (var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(_server) { Database = "postgres" }.ConnectionString))
        {
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        foreach (var folder in new[] { _packages, _official }.Where(Directory.Exists))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private string ConnectionString => new NpgsqlConnectionStringBuilder(_server) { Database = _database }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", ConnectionString);
        builder.UseSetting("Messaging:Transport", "InMemory");
        builder.UseSetting("Jwt:Authority", Issuer);
        builder.UseSetting("HomeMix:PackagesPath", _packages);
        builder.UseSetting("HomeMix:OfficialPath", _official);
        builder.UseSetting("HomeMix:NotificationsPerHour", "1000");
        builder.UseSetting("HomeMix:Plugins", "todo, chat");
        builder.UseSetting("HomeMix:Blocked:0", "evil");
        builder.UseSetting("HomeMix:MaxDocumentBytes", "4096");
        builder.UseSetting("HomeMix:MaxInstallationBytes", "65536");
        builder.UseSetting("HomeMix:MaxDocumentsPerCollection", "3");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(Households);
            services.RemoveAll<IHouseholdRoles>();
            services.AddScoped<IHouseholdRoles, FakeHouseholdRoles>();
            services.RemoveAll<IGitHubReleases>();
            services.AddSingleton<IGitHubReleases>(GitHub);
            services.RemoveAll<IStoreIndex>();
            services.AddSingleton<IStoreIndex>(Store);
            services.AddHttpClient(ScriptBridge.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => Web);
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, bearer =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(_key);
                bearer.Configuration = configuration;
                bearer.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });
        });
    }

    public HttpClient SignedIn(Guid userId) => ClientWith(Token(userId, actor: null));

    /// <summary>Someone with this server role (User, Admin or Owner).</summary>
    public HttpClient WithRole(Guid userId, string role) => ClientWith(Token(userId, actor: null, role));

    /// <summary>An assistant acting for the person (jarvis-v1.md, JV3).</summary>
    public HttpClient Assistant(Guid userId) => ClientWith(Token(userId, "jarvis-service"));

    public HttpClient ClientWith(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>A new household where <paramref name="admin"/> has the role Admin.</summary>
    public Guid Household(Guid admin)
    {
        var household = Guid.NewGuid();
        Households.Set(household, admin, HouseholdRole.Admin);
        return household;
    }

    private string Token(Guid userId, string? actor, string role = "User")
    {
        var now = DateTime.UtcNow;
        var claims = new Dictionary<string, object> { ["sub"] = userId.ToString(), ["role"] = role };
        if (actor is not null)
        {
            claims["act"] = new Dictionary<string, object> { ["sub"] = actor };
            claims["amr"] = new[] { "delegated" };
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = "homeapps",
            TokenType = "at+jwt",
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = claims,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        });
    }

    private sealed class NoPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
