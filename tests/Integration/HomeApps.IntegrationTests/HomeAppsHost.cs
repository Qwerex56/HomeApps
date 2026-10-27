extern alias AccountsApi;
extern alias AuthApi;

using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Auth.Infrastructure.Emails;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace HomeApps.IntegrationTests;

/// <summary>
/// Auth and Accounts as they run together: each its own database on one Postgres, both on one RabbitMQ, emails
/// recorded instead of sent. Accounts reads Auth's keys through Auth's test server, the way it reads them from
/// <c>http://auth:8080</c> in Docker (Q10), so every token is a real one signed by Auth. Postgres and RabbitMQ come
/// from <c>AUTH_TEST_DATABASE</c> and <c>AUTH_TEST_RABBITMQ</c> when set, otherwise from Testcontainers.
/// The configured Owner is invited at start-up, as on a fresh server (A-30).
/// </summary>
public sealed class HomeAppsHost : IAsyncLifetime
{
    public const string Client = "jarvis";
    public const string Password = "correct horse battery";
    public static readonly string OwnerEmail = Unique("owner");

    private readonly List<IAsyncDisposable> _containers = [];
    private readonly List<string> _databases = [];
    private string _postgres = string.Empty;
    private string _rabbitHost = string.Empty;
    private ushort _rabbitPort = 5672;

    public AuthHost Auth { get; private set; } = null!;

    public AccountsHost Accounts { get; private set; } = null!;

    public RecordedEmails Emails { get; } = new();

    public async Task InitializeAsync()
    {
        _postgres = Environment.GetEnvironmentVariable("AUTH_TEST_DATABASE") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_postgres))
        {
            var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await StartAsync(postgres);
            _postgres = postgres.GetConnectionString();
        }

        _rabbitHost = Environment.GetEnvironmentVariable("AUTH_TEST_RABBITMQ") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_rabbitHost))
        {
            var rabbit = new RabbitMqBuilder("rabbitmq:3-management").WithUsername("guest").WithPassword("guest").Build();
            await StartAsync(rabbit);
            _rabbitHost = rabbit.Hostname;
            _rabbitPort = rabbit.GetMappedPublicPort(5672);
        }

        Auth = new AuthHost(this, Database("auth"));
        Accounts = new AccountsHost(this, Database("accounts"));

        // Start Auth first: Accounts asks it for the keys, and both create their queues on start.
        _ = Auth.Server;
        _ = Accounts.Server;
    }

    public async Task DisposeAsync()
    {
        await Accounts.DisposeAsync();
        await Auth.DisposeAsync();

        NpgsqlConnection.ClearAllPools();
        await using (var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(_postgres) { Database = "postgres" }.ConnectionString))
        {
            await connection.OpenAsync();
            foreach (var database in _databases)
            {
                await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection);
                await drop.ExecuteNonQueryAsync();
            }
        }

        foreach (var container in _containers)
        {
            await container.DisposeAsync();
        }
    }

    public static string Unique(string name) => $"{name}.{Guid.NewGuid():N}@example.com";

    /// <summary>Waits for something that travels through the outbox and RabbitMQ.</summary>
    public static async Task<T> EventuallyAsync<T>(Func<Task<T?>> probe, string what)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (await probe() is { } found)
            {
                return found;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Gave up waiting for {what}.");
    }

    private string Database(string service)
    {
        var name = $"{service}_integration_{Guid.NewGuid():N}";
        _databases.Add(name);
        return new NpgsqlConnectionStringBuilder(_postgres) { Database = name }.ConnectionString;
    }

    private void UseMessaging(IWebHostBuilder builder, string connectionString)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", connectionString);
        builder.UseSetting("Messaging:Transport", "RabbitMq");
        builder.UseSetting("RabbitMq:Host", _rabbitHost);
        builder.UseSetting("RabbitMq:Port", _rabbitPort.ToString(CultureInfo.InvariantCulture));
    }

    private async Task StartAsync(IContainer container)
    {
        _containers.Add(container);
        await container.StartAsync();
    }

    /// <summary>The real Auth Api, signing with a fresh key and recording its emails.</summary>
    public sealed class AuthHost(HomeAppsHost host, string connectionString) : WebApplicationFactory<AuthApi::Program>
    {
        private readonly string _keysPath = Path.Combine(Path.GetTempPath(), $"homeapps-integration-{Guid.NewGuid():N}");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            using var rsa = RSA.Create(2048);
            host.UseMessaging(builder, connectionString);
            builder.UseSetting("Auth:Signing:PrivateKeyPem", rsa.ExportPkcs8PrivateKeyPem());
            builder.UseSetting("Auth:DataProtection:KeysPath", _keysPath);
            builder.UseSetting("Auth:Email:Smtp:Host", string.Empty);
            builder.UseSetting("Auth:Bootstrap:Email", OwnerEmail);
            builder.UseSetting("Auth:RateLimits:PerMinute", "1000");
            builder.UseSetting("Auth:RateLimits:MailPerHour", "1000");
            builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(host.Emails));
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            if (Directory.Exists(_keysPath))
            {
                Directory.Delete(_keysPath, recursive: true);
            }
        }
    }

    /// <summary>The real Accounts Api, trusting Auth's keys exactly as in Docker, only over the test server.</summary>
    public sealed class AccountsHost(HomeAppsHost host, string connectionString) : WebApplicationFactory<AccountsApi::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            host.UseMessaging(builder, connectionString);
            builder.UseSetting("Jwt:Authority", "http://auth");
            builder.UseSetting("Jwt:Issuer", "https://app.localhost");
            builder.UseSetting("Jwt:RequireHttpsMetadata", "false");
            builder.UseSetting("Accounts:RateLimits:Changes", "1000");
            builder.ConfigureTestServices(services =>
            {
                foreach (var scheme in new[] { JwtBearerDefaults.AuthenticationScheme, "ReactivationTicket" })
                {
                    services.PostConfigure<JwtBearerOptions>(scheme, bearer =>
                        bearer.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                            "http://auth/.well-known/openid-configuration",
                            new OpenIdConnectConfigurationRetriever(),
                            new HttpDocumentRetriever(new HttpClient(host.Auth.Server.CreateHandler())) { RequireHttps = false }));
                }
            });
        }
    }
}

/// <summary>Every email Auth sent, in place of SMTP.</summary>
public sealed partial class RecordedEmails : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>The token from the newest link sent to <paramref name="to"/> (the last part of the path or <c>?token=</c>).</summary>
    public Task<string> TokenSentToAsync(string to) => HomeAppsHost.EventuallyAsync(() =>
    {
        var message = _sent.LastOrDefault(m => string.Equals(m.To, to, StringComparison.OrdinalIgnoreCase));
        var link = message is null ? null : Link().Match(message.Text);
        return Task.FromResult(link is { Success: true } ? link.Groups["token"].Value : null);
    }, $"an email to {to}");

    [GeneratedRegex(@"https://\S+?(?:token=|/invite/)(?<token>[A-Za-z0-9_\-]+)")]
    private static partial Regex Link();
}

internal static class Http
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    public static HttpRequestMessage Bearer(this HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string path, string token, object? body = null) =>
        client.SendAsync(new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        }.Bearer(token));
}
