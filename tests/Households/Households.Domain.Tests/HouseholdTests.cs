using Households.Domain.Households;
using Households.Domain.Households.Events;

namespace Households.Domain.Tests;

public sealed class HouseholdTests
{
    private readonly FixedTime _time = new(A.Now);

    [Fact]
    public void H1_CreatorIsTheOnlyOwner()
    {
        var ownerId = Guid.NewGuid();

        var household = Household.Create(Guid.NewGuid(), ownerId, " Dom ", " Nasz dom ", _time).Value;

        Assert.Equal("Dom", household.Name.Value);
        Assert.Equal("Nasz dom", household.Description!.Value);
        Assert.Equal(HouseholdStatus.Active, household.Status);
        var owner = Assert.Single(household.Members);
        Assert.Equal((ownerId, HouseholdRole.Owner, A.Now), (owner.UserId, owner.Role, owner.JoinedAt));
        var created = Assert.IsType<HouseholdCreated>(Assert.Single(household.DomainEvents));
        Assert.Equal((household.Id, ownerId, "Dom"), (created.HouseholdId, created.OwnerId, created.Name));
    }

    [Fact]
    public void H1_InvalidNameAndDescriptionAreBothReported()
    {
        var result = Household.Create(Guid.NewGuid(), Guid.NewGuid(), "", new string('x', 200), _time);

        Assert.Equal(["household.name_invalid", "household.description_invalid"],
            result.Errors.Select(e => (string)e.Metadata["Code"]).ToArray());
    }

    [Theory]
    [InlineData("owner", true)]
    [InlineData("admin", true)]
    [InlineData("member", false)]
    [InlineData("guest", false)]
    public void H4_OwnerAndAdminRename(string who, bool allowed)
    {
        var h = new AHousehold(_time);

        var result = h.Household.Update(h.Of(who), "Domek", "Letni", _time);

        Assert.Equal(allowed, result.IsSuccess);
        if (allowed)
        {
            Assert.Equal("Domek", h.Household.Name.Value);
            var updated = Assert.IsType<HouseholdUpdated>(Assert.Single(h.Household.DomainEvents));
            Assert.Equal(("Domek", "Letni"), (updated.Name, updated.Description));
        }
        else
        {
            Assert.Equal("household.forbidden", result.Code());
        }
    }

    [Fact]
    public void H4_NoChangeRaisesNoEvent()
    {
        var h = new AHousehold(_time);

        Assert.True(h.Household.Update(h.Owner, "Dom", null, _time).IsSuccess);

        Assert.Empty(h.Household.DomainEvents);
    }

    [Fact]
    public void Q3_StrangerGetsNotFoundNotForbidden()
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.not_found", h.Household.Update(h.Stranger, "X", null, _time).Code());
        Assert.False(h.Household.CanRead(h.Stranger, _time.Now));
    }

    [Fact]
    public void D7_OwnerArchivesAndTheHouseholdDisappearsForMembers()
    {
        var h = new AHousehold(_time);

        Assert.True(h.Household.Archive(h.Owner, _time).IsSuccess);

        Assert.Equal(HouseholdStatus.Archived, h.Household.Status);
        Assert.Equal(_time.Now, h.Household.ArchivedAt);
        Assert.IsType<HouseholdArchived>(Assert.Single(h.Household.DomainEvents));
        Assert.False(h.Household.CanRead(h.Owner, _time.Now));
        Assert.False(h.Household.CanRead(h.Member, _time.Now));
        Assert.True(h.Household.CanRead(A.ServerAdmin(), _time.Now));
        Assert.Equal("household.not_found", h.Household.Update(h.Owner, "X", null, _time).Code());
        Assert.Equal("household.not_found", h.Household.Archive(h.Owner, _time).Code());
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("member")]
    public void H5_OnlyTheOwnerArchives(string who)
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.forbidden", h.Household.Archive(h.Of(who), _time).Code());
    }

    [Fact]
    public void D8_ServerAdminArchivesAndRestoresWithoutBeingAMember()
    {
        var h = new AHousehold(_time);
        var serverAdmin = A.ServerAdmin();

        Assert.True(h.Household.Archive(serverAdmin, _time).IsSuccess);
        Assert.Equal("household.archived", h.Household.Archive(serverAdmin, _time).Code());
        Assert.True(h.Household.Restore(serverAdmin, _time).IsSuccess);

        Assert.Equal(HouseholdStatus.Active, h.Household.Status);
        Assert.Null(h.Household.ArchivedAt);
        Assert.IsType<HouseholdRestored>(h.Household.DomainEvents[^1]);
        Assert.Equal("household.not_archived", h.Household.Restore(serverAdmin, _time).Code());
    }

    [Fact]
    public void D7_OwnerCannotRestore()
    {
        var h = new AHousehold(_time);
        h.Household.Archive(h.Owner, _time);

        Assert.Equal("household.not_found", h.Household.Restore(h.Owner, _time).Code());
    }

    [Fact]
    public void D2a_GuestReadsTheHouseholdButNotTheMemberList()
    {
        var h = new AHousehold(_time);

        Assert.True(h.Household.CanRead(h.Guest, _time.Now));
        Assert.False(h.Household.CanSeeMembers(h.Guest, _time.Now));
        Assert.True(h.Household.CanSeeMembers(h.Member, _time.Now));
        Assert.True(h.Household.CanSeeMembers(A.ServerAdmin(), _time.Now));
        Assert.False(h.Household.CanSeeMembers(h.Stranger, _time.Now));
    }
}
