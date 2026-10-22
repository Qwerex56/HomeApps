using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using HomeApps.Contracts.Auth;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Auth.IntegrationTests;

/// <summary>
/// Auth with everything real except Google: Postgres, RabbitMQ and SMTP (Mailpit). Each one comes from an environment
/// variable when set (<c>AUTH_TEST_DATABASE</c>, <c>AUTH_TEST_RABBITMQ</c> host, <c>AUTH_TEST_MAILPIT</c> host with
/// SMTP on 1025 and the API on 8025), otherwise from Testcontainers. A second bus plays Accounts.
/// </summary>
public class AuthHost : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string WebClient = "homeapps-web";
    public const string NativeClient = "jarvis";
    public const string Password = "correct horse battery";

    private readonly List<IAsyncDisposable> _containers = [];
    private readonly string _keysPath = Path.Combine(Path.GetTempPath(), $"auth-integration-{Guid.NewGuid():N}");
    private readonly string _database = $"auth_integration_{Guid.NewGuid():N}";
    private string _postgres = string.Empty;
    private string _rabbitHost = string.Empty;
    private ushort _rabbitPort = 5672;
    private string _smtpHost = string.Empty;
    private ushort _smtpPort = 1025;
    private IBusControl? _accounts;

    public Mailpit Mail { get; private set; } = null!;

    /// <summary>What Accounts received from Auth.</summary>
    public ConcurrentQueue<IdentityRegistered> Registered { get; } = new();

    protected virtual string? BootstrapEmail => null;

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

        var mailHost = Environment.GetEnvironmentVariable("AUTH_TEST_MAILPIT");
        var mailApi = new Uri($"http://{mailHost}:8025");
        if (string.IsNullOrWhiteSpace(mailHost))
        {
            var mailpit = new ContainerBuilder("axllent/mailpit:v1.27")
                .WithPortBinding(1025, true)
                .WithPortBinding(8025, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8025).ForPath("/api/v1/info")))
                .Build();
            await StartAsync(mailpit);
            mailHost = mailpit.Hostname;
            _smtpPort = mailpit.GetMappedPublicPort(1025);
            mailApi = new Uri($"http://{mailHost}:{mailpit.GetMappedPublicPort(8025)}");
        }

        _smtpHost = mailHost;
        Mail = new Mailpit(mailApi);

        _accounts = Bus.Factory.CreateUsingRabbitMq(bus =>
        {
            bus.Host(_rabbitHost, _rabbitPort, "/", host =>
            {
                host.Username("guest");
                host.Password("guest");
            });
            bus.ReceiveEndpoint($"accounts-tests-{Guid.NewGuid():N}", endpoint =>
            {
                endpoint.AutoDelete = true;
                endpoint.Durable = false;
                endpoint.Handler<IdentityRegistered>(context =>
                {
                    Registered.Enqueue(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        await _accounts.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        if (_accounts is not null)
        {
            await _accounts.StopAsync();
        }

        NpgsqlConnection.ClearAllPools();
        await using (var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(_postgres) { Database = "postgres" }.ConnectionString))
        {
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }

        foreach (var container in _containers)
        {
            await container.DisposeAsync();
        }

        Mail.Dispose();
        if (Directory.Exists(_keysPath))
        {
            Directory.Delete(_keysPath, recursive: true);
        }
    }

    /// <summary>Publishes as Accounts does (UserDisabled, UserEnabled, UserErased).</summary>
    public Task PublishFromAccountsAsync<T>(T message)
        where T : class => _accounts!.Publish(message);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        using var rsa = RSA.Create(2048);
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", new NpgsqlConnectionStringBuilder(_postgres) { Database = _database }.ConnectionString);
        builder.UseSetting("Messaging:Transport", "RabbitMq");
        builder.UseSetting("RabbitMq:Host", _rabbitHost);
        builder.UseSetting("RabbitMq:Port", _rabbitPort.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("Auth:Email:Smtp:Host", _smtpHost);
        builder.UseSetting("Auth:Email:Smtp:Port", _smtpPort.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("Auth:Signing:PrivateKeyPem", rsa.ExportPkcs8PrivateKeyPem());
        builder.UseSetting("Auth:DataProtection:KeysPath", _keysPath);
        builder.UseSetting("Auth:RateLimits:PerMinute", "1000");
        builder.UseSetting("Auth:RateLimits:MailPerHour", "1000");
        if (BootstrapEmail is not null)
        {
            builder.UseSetting("Auth:Bootstrap:Email", BootstrapEmail);
        }
    }

    public static string Unique(string name) => $"{name}.{Guid.NewGuid():N}@example.com";

    /// <summary>Waits for something that happens through the outbox and RabbitMQ.</summary>
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

    private async Task StartAsync(IContainer container)
    {
        _containers.Add(container);
        await container.StartAsync();
    }
}

/// <summary>Reads the emails Auth sent, through Mailpit's API.</summary>
public sealed partial class Mailpit(Uri api) : IDisposable
{
    private readonly HttpClient _http = new() { BaseAddress = api };

    /// <summary>The link to the Vue app in the newest email to <paramref name="to"/>.</summary>
    public Task<Uri> LinkSentToAsync(string to) => AuthHost.EventuallyAsync<Uri>(async () =>
    {
        var search = await _http.GetFromJsonAsync<JsonElement>($"/api/v1/search?query={Uri.EscapeDataString($"to:\"{to}\"")}");
        var messages = search.GetProperty("messages");
        if (messages.GetArrayLength() == 0)
        {
            return null;
        }

        var message = await _http.GetFromJsonAsync<JsonElement>($"/api/v1/message/{messages[0].GetProperty("ID").GetString()}");
        var link = AppLink().Match(message.GetProperty("Text").GetString() ?? string.Empty);
        return link.Success ? new Uri(link.Value) : null;
    }, $"an email to {to}");

    public void Dispose() => _http.Dispose();

    [GeneratedRegex(@"https://app\.localhost/\S+")]
    private static partial Regex AppLink();
}

internal static class Json
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;
}
