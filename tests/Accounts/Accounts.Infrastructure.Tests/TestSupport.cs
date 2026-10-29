using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Accounts.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Accounts.Infrastructure.Tests;

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class A
{
    public static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    public static DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);

    public static string Unique(string name) => $"{name}.{Guid.NewGuid():N}@example.com";

    public static Email Email(string value) => Domain.Common.Email.Create(value).Value;

    public static Profile Profile(string displayName = "Adam") =>
        Domain.Users.Profile.Create(
            displayName, "Adam", "Nowak", "Adaś", "https://cdn.example.com/a.png", "pl-PL", "Europe/Warsaw",
            new DateOnly(1990, 5, 1), Today).Value;

    public static Registration Registration() =>
        Domain.Users.Registration.Create(
            RegistrationSource.Google, Now, true, "owner@example.com", "adam@old.example.com", "homeapps-web", "Firefox", "pl-PL,en;q=0.8");

    /// <summary>A new user with domain events cleared and one history entry (Registered).</summary>
    public static User User(TimeProvider time, string? email = null, string displayName = "Adam", ServerRole role = ServerRole.User)
    {
        var user = Domain.Users.User.Register(Guid.NewGuid(), Email(email ?? Unique("adam")), role, Profile(displayName), Registration(), time);
        user.ClearDomainEvents();
        return user;
    }

    public static Actor Owner { get; } = new(Guid.NewGuid(), ServerRole.Owner);
}

/// <summary>
/// One migrated Postgres database for a test class. Uses <c>ACCOUNTS_TEST_DATABASE</c> (a server connection string)
/// when set, otherwise starts Postgres in Docker through Testcontainers.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable("ACCOUNTS_TEST_DATABASE");
        if (string.IsNullOrWhiteSpace(server))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
            server = _container.GetConnectionString();
        }

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(server) { Database = $"accounts_tests_{Guid.NewGuid():N}" };
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

    internal string ConnectionString => _connectionString;

    internal AccountsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AccountsDbContext>().UseNpgsql(_connectionString).Options, new NoPublisher());

    private sealed class NoPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
