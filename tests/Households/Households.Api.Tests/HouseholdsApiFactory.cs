using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Households.Application.AccountsSync;
using Households.Infrastructure.Persistence;
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

namespace Households.Api.Tests;

/// <summary>
/// The real Households Api on its own Postgres database (<c>HOUSEHOLDS_TEST_DATABASE</c>, otherwise Testcontainers)
/// and MassTransit in memory. JWT Bearer trusts a test key instead of Auth's JWKS, so tests sign their own tokens.
/// Users are created the way Accounts' <c>UserCreated</c> creates them: through <see cref="CreateUserCopyCommand"/>.
/// </summary>
public class HouseholdsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://auth.homeapps.test";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test" };
    private PostgreSqlContainer? _container;
    private string _server = string.Empty;
    private string _database = string.Empty;

    protected virtual int Invites => 1000;

    protected virtual int Changes => 1000;

    protected virtual int MaxMembers => 100;

    public async Task InitializeAsync()
    {
        _server = Environment.GetEnvironmentVariable("HOUSEHOLDS_TEST_DATABASE") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_server))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
            _server = _container.GetConnectionString();
        }

        _database = $"households_api_tests_{Guid.NewGuid():N}";
        await using var db = new HouseholdsDbContext(
            new DbContextOptionsBuilder<HouseholdsDbContext>().UseNpgsql(ConnectionString).Options, new NoPublisher());
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
        builder.UseSetting("Households:MaxMembers", MaxMembers.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("Households:RateLimits:Invites", Invites.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("Households:RateLimits:Changes", Changes.ToString(CultureInfo.InvariantCulture));

        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, bearer =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(_key);
                bearer.Configuration = configuration;
                bearer.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }));
    }

    /// <summary>An access token as Auth signs it.</summary>
    public string AccessToken(Guid userId, string role = "User", string audience = "homeapps")
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            TokenType = "at+jwt",
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),
                ["email"] = "someone@example.com",
                ["role"] = role,
            },
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

    /// <summary>Creates the user copy the way Accounts' <c>UserCreated</c> does and returns its id.</summary>
    public async Task<Guid> UserAsync(string displayName = "Adam")
    {
        var id = Guid.NewGuid();
        await SendAsync(new CreateUserCopyCommand(id, displayName, null));
        return id;
    }

    public async Task SendAsync(IRequest<FluentResults.Result> command)
    {
        await using var scope = Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
        if (result.IsFailed)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Message)));
        }
    }

    /// <summary>Creates a household as <paramref name="owner"/> and returns its id.</summary>
    public async Task<Guid> HouseholdAsync(Guid owner, string name = "Dom")
    {
        using var client = SignedIn(owner);
        using var response = await client.PostAsJsonAsync("/households", new { name, description = "Opis" });
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    /// <summary>An invite from <paramref name="by"/>; returns its code as people see it.</summary>
    public async Task<string> InviteAsync(Guid household, Guid by, string role = "Member")
    {
        using var client = SignedIn(by);
        using var response = await client.PostAsJsonAsync($"/households/{household}/invites", new { role });
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).GetProperty("code").GetString()!;
    }

    /// <summary>A new user who joined <paramref name="household"/> through an invite from <paramref name="by"/>.</summary>
    public async Task<Guid> MemberAsync(Guid household, Guid by, string role = "Member", string displayName = "Ewa")
    {
        var code = await InviteAsync(household, by, role);
        var id = await UserAsync(displayName);
        using var client = SignedIn(id);
        using var response = await client.PostAsync($"/invites/{code}/accept", null);
        response.EnsureSuccessStatusCode();
        return id;
    }

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
