using HomeApps.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using NSubstitute;

namespace HomeApps.BuildingBlocks.Tests;

public sealed class UniqueViolationMappingTests
{
    [Fact]
    public async Task A_unique_violation_from_Postgres_becomes_a_concurrency_conflict()
    {
        var cause = Postgres(PostgresErrorCodes.UniqueViolation);
        await using var db = Context(new DbUpdateException("save failed", cause));

        var thrown = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => Save(db));

        Assert.IsType<DbUpdateException>(thrown.InnerException);
        Assert.Same(cause, thrown.InnerException!.InnerException);
    }

    [Fact]
    public async Task Another_database_error_stays_a_DbUpdateException()
    {
        await using var db = Context(new DbUpdateException("save failed", Postgres(PostgresErrorCodes.ForeignKeyViolation)));

        await Assert.ThrowsAsync<DbUpdateException>(() => Save(db));
    }

    [Fact]
    public async Task A_DbUpdateException_without_a_Postgres_cause_stays_as_it_is()
    {
        await using var db = Context(new DbUpdateException("save failed", new InvalidOperationException("other")));

        await Assert.ThrowsAsync<DbUpdateException>(() => Save(db));
    }

    [Fact]
    public async Task A_lost_optimistic_race_is_still_a_concurrency_conflict()
    {
        await using var db = Context(new DbUpdateConcurrencyException("stale"));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => Save(db));
    }

    private static async Task Save(GadgetDbContext db)
    {
        db.Gadgets.Add(Gadget.Create());
        await db.SaveChangesAsync();
    }

    private static PostgresException Postgres(string sqlState) => new("duplicate key", "ERROR", "ERROR", sqlState);

    private static GadgetDbContext Context(Exception failure)
    {
        var options = new DbContextOptionsBuilder<GadgetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new Failing(failure))
            .Options;
        return new GadgetDbContext(options, Substitute.For<IPublisher>());
    }

    private sealed class Failing(Exception failure) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw failure;
    }
}
