using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Accounts.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Tests;

public sealed class PersistenceTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private readonly FixedTime _time = new(A.Now);

    private async Task<User> Saved(User user)
    {
        await using var db = database.CreateContext();
        new UserRepository(db).Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<User?> Load(Guid id)
    {
        await using var db = database.CreateContext();
        return await new UserRepository(db).GetByIdAsync(id, default);
    }

    [Fact]
    public async Task Whole_user_round_trips()
    {
        var user = A.User(_time);
        user.Disable(new Actor(user.Id, user.Role), "Przerwa", _time);
        user.RecordSeen(A.Now.AddMinutes(5));
        await Saved(user);

        var loaded = await Load(user.Id);

        Assert.NotNull(loaded);
        Assert.Equal(user.Email, loaded.Email);
        Assert.Equal(user.Role, loaded.Role);
        Assert.Equal(user.Profile, loaded.Profile);
        Assert.Equal(user.Registration, loaded.Registration);
        Assert.Equal(AccountStatus.Disabled, loaded.Status);
        Assert.Equal(user.Disabling, loaded.Disabling);
        Assert.True(loaded.IsDisabledBySelf);
        Assert.Equal(user.CreatedAt, loaded.CreatedAt);
        Assert.Equal(user.UpdatedAt, loaded.UpdatedAt);
        Assert.Equal(A.Now.AddMinutes(5), loaded.LastSeenAt);
        Assert.Empty(loaded.History);
    }

    [Fact]
    public async Task Minimal_profile_and_registration_round_trip()
    {
        var user = User.Register(
            Guid.NewGuid(), A.Email(A.Unique("ola")), ServerRole.Admin,
            Profile.FromRegistration(null, null, null, null, null),
            Registration.Create(RegistrationSource.Password, A.Now, false, null, null, null, null, null), _time);
        await Saved(user);

        var loaded = await Load(user.Id);

        Assert.NotNull(loaded);
        Assert.Equal(user.Profile, loaded.Profile);
        Assert.Null(loaded.Profile.AvatarUrl);
        Assert.Equal(user.Registration, loaded.Registration);
        Assert.Null(loaded.Disabling);
    }

    [Fact]
    public async Task Entries_appended_to_a_user_loaded_without_history_are_saved()
    {
        var user = await Saved(A.User(_time));

        await using (var db = database.CreateContext())
        {
            var loaded = await new UserRepository(db).GetByIdAsync(user.Id, default);
            _time.Now = A.Now.AddHours(1);
            loaded!.Disable(A.Owner, "Spam", _time);
            _time.Now = A.Now.AddHours(2);
            loaded.Enable(A.Owner, _time);
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var history = await new UserDirectory(read).HistoryAsync(user.Id, null, 10, default);
        Assert.Equal([HistoryAction.Enabled, HistoryAction.Disabled, HistoryAction.Registered], history.Select(h => h.Action));
        Assert.Equal("Spam", history[1].Reason);
        Assert.Equal(A.Owner.Id, history[1].ActorId);
        Assert.Equal(AccountStatus.Active, (await Load(user.Id))!.Status);
    }

    [Fact]
    public async Task Erase_removes_the_user_with_history_and_leaves_a_trace()
    {
        var user = A.User(_time);
        user.Disable(A.Owner, null, _time);
        await Saved(user);

        await using (var db = database.CreateContext())
        {
            var users = new UserRepository(db);
            var loaded = await users.GetByIdAsync(user.Id, default);
            var erased = loaded!.Erase(A.Owner, user.Email.Value, _time).Value;
            users.Remove(loaded);
            users.AddErased(erased);
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        Assert.Null(await new UserRepository(read).GetByIdAsync(user.Id, default));
        Assert.True(await new UserRepository(read).IsErasedAsync(user.Id, default));
        Assert.False(await read.History.AnyAsync(h => h.UserId == user.Id));
        var trace = await read.ErasedUsers.SingleAsync(e => e.Id == user.Id);
        Assert.Equal(A.Owner.Id, trace.ErasedBy);
    }

    [Fact]
    public async Task Email_is_unique()
    {
        var email = A.Unique("same");
        await Saved(A.User(_time, email));

        await Assert.ThrowsAsync<DbUpdateException>(() => Saved(A.User(_time, email)));
    }

    [Fact]
    public async Task Lost_race_is_a_concurrency_conflict()
    {
        var user = await Saved(A.User(_time));
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var a = await new UserRepository(first).GetByIdAsync(user.Id, default);
        var b = await new UserRepository(second).GetByIdAsync(user.Id, default);

        a!.UpdateProfile(new Actor(user.Id, user.Role), A.Profile("Adam Pierwszy"), _time);
        await first.SaveChangesAsync();
        b!.UpdateProfile(new Actor(user.Id, user.Role), A.Profile("Adam Drugi"), _time);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => second.SaveChangesAsync());
    }
}
