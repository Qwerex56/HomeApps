using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Accounts.Domain.Users.Events;

namespace Accounts.Domain.Tests;

public sealed class ProfileTests
{
    private readonly FixedTime _time = new(A.Now);

    [Theory]
    [InlineData(ServerRole.User, ServerRole.User, false)]
    [InlineData(ServerRole.User, ServerRole.Admin, false)]
    [InlineData(ServerRole.User, ServerRole.Owner, false)]
    [InlineData(ServerRole.Admin, ServerRole.User, true)]
    [InlineData(ServerRole.Admin, ServerRole.Admin, false)]
    [InlineData(ServerRole.Admin, ServerRole.Owner, false)]
    [InlineData(ServerRole.Owner, ServerRole.User, true)]
    [InlineData(ServerRole.Owner, ServerRole.Admin, true)]
    [InlineData(ServerRole.Owner, ServerRole.Owner, false)]
    public void R1_SomeoneElsesAccountNeedsAHigherRole(ServerRole actor, ServerRole target, bool allowed)
    {
        var user = AUser.Registered(_time, target);

        Assert.Equal(allowed, user.CanBeManagedBy(AUser.Someone(actor)));
    }

    [Theory]
    [InlineData(ServerRole.User)]
    [InlineData(ServerRole.Admin)]
    [InlineData(ServerRole.Owner)]
    public void R1_EveryoneManagesTheirOwnAccount(ServerRole role)
    {
        var user = AUser.Registered(_time, role);

        Assert.True(user.CanBeManagedBy(user.Self()));
    }

    [Fact]
    public void UC02_UserChangesOwnProfile()
    {
        var user = AUser.Registered(_time);
        _time.Now = A.Now.AddHours(1);

        var result = user.UpdateProfile(user.Self(), A.Profile("Adam N."), _time);

        Assert.True(result.IsSuccess);
        Assert.Equal("Adam N.", user.Profile.DisplayName.Value);
        Assert.Equal(A.Now.AddHours(1), user.UpdatedAt);
        var entry = user.History[^1];
        Assert.Equal(HistoryAction.ProfileUpdated, entry.Action);
        Assert.Equal(user.Id, entry.ActorId);
        Assert.Equal("DisplayName", entry.Details);
        Assert.Equal("Adam N.", Assert.IsType<UserProfileUpdated>(Assert.Single(user.DomainEvents)).DisplayName);
    }

    [Fact]
    public void UC13_AdminChangesUsersProfile()
    {
        var user = AUser.Registered(_time);
        var admin = AUser.Someone(ServerRole.Admin);

        Assert.True(user.UpdateProfile(admin, A.Profile("Adaś"), _time).IsSuccess);
        Assert.Equal(admin.Id, user.History[^1].ActorId);
    }

    [Fact]
    public void R1_AdminCannotChangeAnotherAdminsProfile()
    {
        var other = AUser.Registered(_time, ServerRole.Admin);

        var result = other.UpdateProfile(AUser.Someone(ServerRole.Admin), A.Profile("Hacked"), _time);

        Assert.Equal("forbidden.role", result.Code());
        Assert.Equal("Adam", other.Profile.DisplayName.Value);
    }

    [Fact]
    public void R1_NobodyButTheOwnerChangesTheOwnersProfile()
    {
        var owner = AUser.Registered(_time, ServerRole.Owner);

        Assert.Equal("forbidden.role", owner.UpdateProfile(AUser.Someone(ServerRole.Owner), A.Profile("X Y"), _time).Code());
        Assert.True(owner.UpdateProfile(owner.Self(), A.Profile("Szef"), _time).IsSuccess);
    }

    [Fact]
    public void UnchangedProfileLeavesNoTrace()
    {
        var user = AUser.Registered(_time);

        Assert.True(user.UpdateProfile(user.Self(), A.Profile(), _time).IsSuccess);
        Assert.Single(user.History);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void ChangeOthersDoNotCopyGoesOnlyToHistory()
    {
        var user = AUser.Registered(_time);
        var profile = Profile.Create("Adam", "Adam", "Nowak", null, null, "en-GB", "Europe/London", null, A.Today).Value;

        user.UpdateProfile(user.Self(), profile, _time);

        Assert.Equal("Locale, TimeZone", user.History[^1].Details);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void AvatarChangeIsPublished()
    {
        var user = AUser.Registered(_time);

        user.UpdateProfile(user.Self(), A.Profile(avatarUrl: "https://example.com/a.png"), _time);

        Assert.Equal(new Uri("https://example.com/a.png"), Assert.IsType<UserProfileUpdated>(Assert.Single(user.DomainEvents)).AvatarUrl);
    }
}
