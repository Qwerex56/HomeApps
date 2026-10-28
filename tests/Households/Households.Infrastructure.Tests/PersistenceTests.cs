using Households.Domain.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using Households.Domain.Users;
using Households.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Households.Infrastructure.Tests;

public sealed class PersistenceTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private readonly FixedTime _time = new(A.Now);

    private async Task Save(Household household, params Invite[] invites)
    {
        await using var db = database.CreateContext();
        new HouseholdRepository(db).Add(household);
        foreach (var invite in invites)
        {
            new InviteRepository(db).Add(invite);
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task WholeHouseholdRoundTripsWithItsMembers()
    {
        var (household, owner, member, invite) = A.Household(_time);
        household.SetNickname(A.User(member), member, "Tata", _time);
        var guestCode = household.RegenerateGuestLink(A.User(owner), _time).Value;
        await Save(household, invite);

        await using var db = database.CreateContext();
        var repository = new HouseholdRepository(db);
        var loaded = await repository.GetByIdAsync(household.Id, default);

        Assert.NotNull(loaded);
        Assert.Equal(household.Name, loaded.Name);
        Assert.Equal(household.Description, loaded.Description);
        Assert.Equal(guestCode, loaded.GuestCode);
        Assert.Equal(household.CreatedAt, loaded.CreatedAt);
        Assert.Equal(owner, loaded.Owner!.UserId);
        var loadedMember = loaded.MemberOf(member)!;
        Assert.Equal((HouseholdRole.Member, "Tata", owner), (loadedMember.Role, loadedMember.Nickname!.Value, loadedMember.InvitedBy));
        Assert.Equal(household.Id, (await repository.GetByGuestCodeAsync(guestCode, default))!.Id);

        var loadedInvite = await new InviteRepository(db).GetByCodeAsync(InviteCode.Parse(invite.Code.Display).Value, default);
        Assert.Equal((invite.Id, 1, HouseholdRole.Member), (loadedInvite!.Id, loadedInvite.Uses, loadedInvite.Role));
    }

    [Fact]
    public async Task MembershipChangesAreSavedThroughTheAggregate()
    {
        var (household, owner, member, invite) = A.Household(_time);
        await Save(household, invite);

        await using (var db = database.CreateContext())
        {
            var loaded = (await new HouseholdRepository(db).GetByIdAsync(household.Id, default))!;
            Assert.True(loaded.TransferOwnership(A.User(owner), member, _time).IsSuccess);
            Assert.True(loaded.RemoveMember(A.User(member), owner, _time).IsSuccess);
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var after = (await new HouseholdRepository(read).GetByIdAsync(household.Id, default))!;
        Assert.Equal(member, Assert.Single(after.Members).UserId);
        Assert.Equal(HouseholdRole.Owner, after.Members[0].Role);
    }

    [Fact]
    public async Task DatabaseRefusesASecondOwner()
    {
        var (household, _, member, invite) = A.Household(_time);
        await Save(household, invite);

        await using var db = database.CreateContext();
        var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Memberships\" SET \"Role\" = 'Owner' WHERE \"HouseholdId\" = {0} AND \"UserId\" = {1}", household.Id, member));
        Assert.Equal("IX_Memberships_OneOwner", error.ConstraintName);
    }

    [Fact]
    public async Task D10_TwoJoinsRacingForTheSameHouseholdCannotBothWin()
    {
        // Through the guest link, so no invite row is shared: only the household's xmin can stop the second join.
        var (household, owner, _, invite) = A.Household(_time);
        var code = household.RegenerateGuestLink(A.User(owner), _time).Value;
        await Save(household, invite);

        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var a = (await new HouseholdRepository(first).GetByIdAsync(household.Id, default))!;
        var b = (await new HouseholdRepository(second).GetByIdAsync(household.Id, default))!;
        Assert.True(a.JoinAsGuest(Guid.NewGuid(), code, 3, _time).IsSuccess);
        Assert.True(b.JoinAsGuest(Guid.NewGuid(), code, 3, _time).IsSuccess);

        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => second.SaveChangesAsync());

        await using var read = database.CreateContext();
        Assert.Equal(3, (await new HouseholdRepository(read).GetByIdAsync(household.Id, default))!.Members.Count);
    }

    [Fact]
    public async Task ListsForAccountEventsAndTheGuestCleanUp()
    {
        var (household, owner, member, invite) = A.Household(_time);
        var guest = Guid.NewGuid();
        var guestInvite = household.CreateInvite(A.User(member), HouseholdRole.Guest, InviteValidity.OneDay, A.Now.AddHours(1), _time).Value;
        household.Accept(guestInvite, guest, 100, _time);
        household.ClearDomainEvents();
        await Save(household, invite, guestInvite);

        await using var db = database.CreateContext();
        var households = new HouseholdRepository(db);
        Assert.Contains(household.Id, (await households.ListInvolvingAsync(owner, default)).Select(h => h.Id));
        Assert.Contains(household.Id, (await households.ListInvolvingAsync(guest, default)).Select(h => h.Id));
        Assert.Empty(await households.ListInvolvingAsync(Guid.NewGuid(), default));
        Assert.DoesNotContain(household.Id, (await households.ListWithExpiredGuestsAsync(A.Now, default)).Select(h => h.Id));
        Assert.Contains(household.Id, (await households.ListWithExpiredGuestsAsync(A.Now.AddHours(1), default)).Select(h => h.Id));

        var invites = new InviteRepository(db);
        Assert.Equal([guestInvite.Id], (await invites.ListNotRevokedAsync(household.Id, member, default)).Select(i => i.Id));
        Assert.Equal(2, (await invites.ListNotRevokedAsync(household.Id, null, default)).Count);
        Assert.Equal([guestInvite.Id], (await invites.ListByCreatorAsync(member, default)).Select(i => i.Id));
    }

    [Fact]
    public async Task UserCopiesRoundTrip()
    {
        var copy = UserCopy.Create(Guid.NewGuid(), "Ania", "https://example.com/a.png");
        copy.Disable();
        await using (var db = database.CreateContext())
        {
            new UserCopies(db).Add(copy);
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var many = await new UserCopies(read).GetManyAsync([copy.Id, Guid.NewGuid()], default);
        var loaded = Assert.Single(many).Value;
        Assert.Equal(("Ania", "https://example.com/a.png", false), (loaded.DisplayName, loaded.AvatarUrl, loaded.IsActive));
    }

    [Fact]
    public async Task H2_MyHouseholdsAndQ7_AdminList()
    {
        var name = $"Dom {Guid.NewGuid():N}"[..20];
        var (household, owner, member, invite) = A.Household(_time, name);
        var (archived, archivedOwner, _, archivedInvite) = A.Household(_time, name + "x");
        archived.Archive(A.User(archivedOwner), _time);
        archived.ClearDomainEvents();
        await Save(household, invite);
        await Save(archived, archivedInvite);
        await using (var db = database.CreateContext())
        {
            new UserCopies(db).Add(UserCopy.Create(owner, "Ola", null));
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var directory = new HouseholdDirectory(read);
        var mine = Assert.Single(await directory.MyHouseholdsAsync(member, A.Now, default));
        Assert.Equal((household.Id, HouseholdRole.Member, 2), (mine.Id, mine.MyRole, mine.MemberCount));
        Assert.Empty(await directory.MyHouseholdsAsync(archivedOwner, A.Now, default));

        var found = await directory.AllHouseholdsAsync(name[..10].ToUpperInvariant(), null, 50, default);
        Assert.Equal(2, found.Count);
        var listed = found.Single(h => h.Id == household.Id);
        Assert.Equal((owner, "Ola", 2, HouseholdStatus.Active), (listed.OwnerId!.Value, listed.OwnerName, listed.MemberCount, listed.Status));
        Assert.Equal(HouseholdStatus.Archived, found.Single(h => h.Id == archived.Id).Status);
        Assert.Empty(await directory.AllHouseholdsAsync("100%_nie_ma", null, 50, default));
        Assert.Empty(await directory.AllHouseholdsAsync(name, A.Now.AddYears(-1), 50, default));
    }
}
