using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Accounts.Api.Http;
using Accounts.Application.AuthSync;
using Accounts.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;
using Contracts = HomeApps.Contracts.Auth;

namespace Accounts.Api.Tests;

/// <summary>
/// The real Accounts Api on its own Postgres database (<c>ACCOUNTS_TEST_DATABASE</c>, otherwise Testcontainers) and
/// MassTransit in memory. Instead of Auth's JWKS both JWT schemes trust a test key, so tests sign their own access
/// tokens and tickets. Accounts are created the way Auth creates them: through <see cref="RegisterUserCommand"/>.
/// </summary>
public class AccountsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://auth.homeapps.test";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test" };
    private PostgreSqlContainer? _container;
    private string _server = string.Empty;
    private string _database = string.Empty;

    protected virtual int Changes => 1000;

    public async Task InitializeAsync()
    {
        _server = Environment.GetEnvironmentVariable("ACCOUNTS_TEST_DATABASE") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_server))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
            _server = _container.GetConnectionString();
        }

        _database = $"accounts_api_tests_{Guid.NewGuid():N}";
        await using var db = new AccountsDbContext(
            new DbContextOptionsBuilder<AccountsDbContext>().UseNpgsql(ConnectionString).Options, new NoPublisher());
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
        builder.UseSetting("Accounts:RateLimits:Changes", Changes.ToString(System.Globalization.CultureInfo.InvariantCulture));

        builder.ConfigureTestServices(services =>
        {
            foreach (var scheme in new[] { JwtBearerDefaults.AuthenticationScheme, ReactivationTicket.Scheme })
            {
                services.PostConfigure<JwtBearerOptions>(scheme, bearer =>
                {
                    var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                    configuration.SigningKeys.Add(_key);
                    bearer.Configuration = configuration;
                    bearer.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                });
            }
        });
    }

    /// <summary>An access token as Auth signs it.</summary>
    public string AccessToken(Guid userId, string role = "User", string audience = "homeapps") =>
        Sign("at+jwt", audience, new Dictionary<string, object>
        {
            ["sub"] = userId.ToString(),
            ["email"] = "someone@example.com",
            ["role"] = role,
        });

    /// <summary>A reactivation ticket as Auth signs it (or, with another scope, some other ticket).</summary>
    public string Ticket(Guid userId, string scope = ReactivationTicket.Scope) =>
        Sign(ReactivationTicket.TokenType, ReactivationTicket.Audience, new Dictionary<string, object>
        {
            ["sub"] = userId.ToString(),
            ["scope"] = scope,
            ["method"] = "Password",
        });

    private string Sign(string type, string audience, Dictionary<string, object> claims)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            TokenType = type,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = claims,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        });
    }

    public HttpClient ClientWith(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient SignedIn(Guid userId, string role = "User") => ClientWith(AccessToken(userId, role));

    /// <summary>Creates the account the way Auth's <c>IdentityRegistered</c> does and returns its id.</summary>
    public async Task<Guid> RegisterAsync(string? email = null, string displayName = "Adam", Contracts.ServerRole role = Contracts.ServerRole.User)
    {
        var id = Guid.NewGuid();
        var identity = new Contracts.IdentityRegistered(
            id, email ?? Unique("adam"), role, displayName, "Adam", "Nowak", null,
            Contracts.RegistrationSource.Password, DateTimeOffset.UtcNow, false, null, null,
            new Contracts.RegistrationContext("homeapps-web", "Firefox", "pl-PL"));
        await using var scope = Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RegisterUserCommand(identity));
        if (result.IsFailed)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Message)));
        }

        return id;
    }

    public static string Unique(string name) => $"{name}.{Guid.NewGuid():N}@example.com";

    public static object Profile(string displayName = "Adam") => new
    {
        displayName,
        firstName = "Adam",
        lastName = "Nowak",
        preferredName = (string?)null,
        avatarUrl = (string?)null,
        locale = "pl-PL",
        timeZone = "Europe/Warsaw",
        dateOfBirth = "1990-05-01",
    };

    private sealed class NoPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}

internal static class Json
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    public static async Task<string?> CodeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).TryGetProperty("code", out var code) ? code.GetString() : null;
}
