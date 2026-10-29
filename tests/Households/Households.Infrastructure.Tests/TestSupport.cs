using Households.Domain.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using Households.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Households.Infrastructure.Tests;

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class A
{
    public static readonly DateTimeOffset Now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    public static Actor User(Guid id) => new(id, ServerRole.User);

    /// <summary>A household with its Owner and one member who joined through the Owner's invite; events cleared.</summary>
    public static (Household Household, Guid Owner, Guid Member, Invite Invite) Household(TimeProvider time, string name = "Dom")
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var household = Domain.Households.Household.Create(Guid.NewGuid(), owner, name, "Opis", time).Value;
        var invite = household.CreateInvite(User(owner), HouseholdRole.Member, InviteValidity.SevenDays, null, time).Value;
        Assert.True(household.Accept(invite, member, 100, time).IsSuccess);
        household.ClearDomainEvents();
        return (household, owner, member, invite);
    }
}

/// <summary>
/// One migrated Postgres database for a test class. Uses <c>HOUSEHOLDS_TEST_DATABASE</c> (a server connection string)
/// when set, otherwise starts Postgres in Docker through Testcontainers.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable("HOUSEHOLDS_TEST_DATABASE");
        if (string.IsNullOrWhiteSpace(server))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
            server = _container.GetConnectionString();
        }

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(server) { Database = $"households_tests_{Guid.NewGuid():N}" };
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

    internal HouseholdsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HouseholdsDbContext>().UseNpgsql(_connectionString).Options, new NoPublisher());

    private sealed class NoPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
