using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Auth.Infrastructure.Tests;

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class A
{
    public static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    public const string Password = "correct horse battery";

    public static Email Email(string value) => Domain.Common.Email.Create(value).Value;

    public static ClientInfo Client(string clientId = "homeapps-web") => ClientInfo.Create(clientId, "Firefox").Value;

    public static Registration Registered(TimeProvider time, string email)
    {
        var registration = Identity.RegisterWithPassword(
            Email(email), Password, RegistrationProfile.Create("Adam", null, null, null).Value,
            new RegistrationContext("homeapps-web", "Firefox", "pl-PL"), Client(), false, true,
            new Security.PasswordHasher(), time).Value;
        registration.Identity.ClearDomainEvents();
        return registration;
    }

    public static string Unique(string name) => $"{name}.{Guid.NewGuid():N}@example.com";
}

/// <summary>
/// One migrated Postgres database for a test class. Uses <c>AUTH_TEST_DATABASE</c> (a server connection string) when
/// set, otherwise starts Postgres in Docker through Testcontainers.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable("AUTH_TEST_DATABASE");
        if (string.IsNullOrWhiteSpace(server))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
            server = _container.GetConnectionString();
        }

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(server) { Database = $"auth_tests_{Guid.NewGuid():N}" };
        _connectionString = builder.ConnectionString;
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using (var db = CreateContext())
        {
            await db.Database.EnsureDeletedAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    internal AuthDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(_connectionString).Options, new NoPublisher());

    private sealed class NoPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
