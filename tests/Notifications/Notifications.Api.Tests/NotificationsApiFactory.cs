using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using MassTransit.Testing;
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
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Notifications.Api.Tests;

/// <summary>
/// The real Notifications Api on its own Postgres database (<c>NOTIFICATIONS_TEST_DATABASE</c>, otherwise
/// Testcontainers), MassTransit in memory and <see cref="FakePushSender"/> instead of real push services.
/// </summary>
public sealed class NotificationsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://auth.homeapps.test";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test" };
    private PostgreSqlContainer? _container;
    private string _server = string.Empty;
    private string _database = string.Empty;

    public FakePushSender Push { get; } = new();

    public ITestHarness Harness => Services.GetTestHarness();

    public async Task InitializeAsync()
    {
        _server = Environment.GetEnvironmentVariable("NOTIFICATIONS_TEST_DATABASE") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_server))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
            _server = _container.GetConnectionString();
        }

        _database = $"notifications_api_tests_{Guid.NewGuid():N}";
        await using var db = new NotificationsDbContext(
            new DbContextOptionsBuilder<NotificationsDbContext>().UseNpgsql(ConnectionString).Options, new NoPublisher());
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
    }

    private string ConnectionString => new NpgsqlConnectionStringBuilder(_server) { Database = _database }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", ConnectionString);
        builder.UseSetting("Messaging:Transport", "InMemory");
        builder.UseSetting("Jwt:Authority", Issuer);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPushSender>();
            services.AddSingleton<IPushSender>(Push);
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, bearer =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(_key);
                bearer.Configuration = configuration;
                bearer.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });
        });
    }

    public HttpClient SignedIn(Guid userId)
    {
        var now = DateTime.UtcNow;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = "homeapps",
            TokenType = "at+jwt",
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = new Dictionary<string, object> { ["sub"] = userId.ToString(), ["role"] = "User" },
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        });
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (iPhone) Safari/605.1");
        return client;
    }

    /// <summary>Turns notifications on for <paramref name="userId"/> on a new device and returns its endpoint.</summary>
    public async Task<string> DeviceAsync(Guid userId, string? endpoint = null)
    {
        endpoint ??= $"https://push.example.com/{Guid.NewGuid():N}";
        using var client = SignedIn(userId);
        using var response = await client.PutAsJsonAsync("/notifications/devices", Subscription(endpoint));
        response.EnsureSuccessStatusCode();
        return endpoint;
    }

    public static object Subscription(string endpoint) => new { endpoint, keys = new { p256dh = Key(65), auth = Key(16) } };

    public static string Key(int bytes) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class NoPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
