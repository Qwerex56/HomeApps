using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Auth.Application.Abstractions;
using Auth.Domain.Identities;
using Auth.Infrastructure.Persistence;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Auth.Api.Tests;

/// <summary>
/// The real Auth Api on its own Postgres database (<c>AUTH_TEST_DATABASE</c>, otherwise Testcontainers), MassTransit
/// in memory and a fake Google. Settings go through <c>UseSetting</c> because Program reads them while registering.
/// </summary>
public class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string WebClient = "homeapps-web";
    public const string NativeClient = "jarvis";
    public const string Password = "correct horse battery";
    public static readonly Uri GoogleAuthorize = new("https://accounts.google.test/o/oauth2/v2/auth");

    private readonly string _keysPath = Path.Combine(Path.GetTempPath(), $"auth-api-tests-{Guid.NewGuid():N}");
    private PostgreSqlContainer? _container;
    private string _server = string.Empty;
    private string _database = string.Empty;

    public FakeGoogle Google { get; } = new();

    protected virtual int PerMinute => 1000;

    protected virtual int RefreshPerMinute => 1000;

    public async Task InitializeAsync()
    {
        _server = Environment.GetEnvironmentVariable("AUTH_TEST_DATABASE") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_server))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
            _server = _container.GetConnectionString();
        }

        _database = $"auth_api_tests_{Guid.NewGuid():N}";
        await using var db = new AuthDbContext(
            new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(ConnectionString).Options, new NoPublisher());
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

        if (Directory.Exists(_keysPath))
        {
            Directory.Delete(_keysPath, recursive: true);
        }
    }

    private string ConnectionString => new NpgsqlConnectionStringBuilder(_server) { Database = _database }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        using var rsa = RSA.Create(2048);
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", ConnectionString);
        builder.UseSetting("Messaging:Transport", "InMemory");
        builder.UseSetting("Auth:Signing:PrivateKeyPem", rsa.ExportPkcs8PrivateKeyPem());
        builder.UseSetting("Auth:DataProtection:KeysPath", _keysPath);
        builder.UseSetting("Auth:Google:ClientId", "google-client");
        builder.UseSetting("Auth:Google:ClientSecret", "google-secret");
        builder.UseSetting("Auth:RateLimits:PerMinute", PerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Auth:RateLimits:RefreshPerMinute", RefreshPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Auth:RateLimits:MailPerHour", "1000");
        builder.UseSetting("Auth:Email:Smtp:Host", string.Empty);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IExternalIdentityVerifier>(Google);
            services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(
                new StaticConfigurationManager<OpenIdConnectConfiguration>(
                    new OpenIdConnectConfiguration { AuthorizationEndpoint = GoogleAuthorize.ToString() }));
        });
    }

    /// <summary>Cookies on, redirects off, over https so the Secure cookies come back.</summary>
    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost"),
    });

    public static string Unique(string name) => $"{name}.{Guid.NewGuid():N}@example.com";

    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string clientId = WebClient) =>
        await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password = Password,
            displayName = "Adam",
            locale = "pl-PL",
            clientId,
        });

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private sealed class NoPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}

/// <summary>Google as the Api sees it: whoever the test says, for the code and verifier it was given.</summary>
public sealed class FakeGoogle : IExternalIdentityVerifier
{
    public ExternalIdentity Identity { get; set; } = new("google-sub", "someone@example.com", true, "Ewa", null, null);

    public string? LastCodeVerifier { get; private set; }

    public Uri? LastRedirectUri { get; private set; }

    public Task<Result<ExternalIdentity>> VerifyAsync(
        ExternalProvider provider, string code, string codeVerifier, Uri redirectUri, CancellationToken cancellationToken)
    {
        LastCodeVerifier = codeVerifier;
        LastRedirectUri = redirectUri;
        return Task.FromResult(Result.Ok(Identity));
    }
}

internal static class Json
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;
}
