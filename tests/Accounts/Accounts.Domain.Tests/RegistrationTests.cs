using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Accounts.Domain.Users.Events;

namespace Accounts.Domain.Tests;

public sealed class RegistrationTests
{
    private readonly FixedTime _time = new(A.Now);

    [Fact]
    public void UC08_RegisterCreatesAnActiveUserWithTheRoleFromAuth()
    {
        var id = Guid.NewGuid();
        var profile = Profile.FromRegistration("Adam", "Adam", "Nowak", "https://lh3.googleusercontent.com/a", "pl-PL");

        var user = User.Register(id, A.Email(), ServerRole.User, profile, AUser.Registration(RegistrationSource.Google), _time);

        Assert.Equal(id, user.Id);
        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Equal(ServerRole.User, user.Role);
        Assert.Null(user.Disabling);
        Assert.Equal(A.Now, user.CreatedAt);
        var registered = Assert.IsType<UserRegistered>(Assert.Single(user.DomainEvents));
        Assert.Equal("Adam", registered.DisplayName);
        Assert.NotNull(registered.AvatarUrl);
    }

    [Fact]
    public void UC09_BootstrapGetsOwnerFromTheEvent()
    {
        var user = User.Register(Guid.NewGuid(), A.Email(), ServerRole.Owner, A.Profile(),
            AUser.Registration(RegistrationSource.Bootstrap), _time);

        Assert.Equal(ServerRole.Owner, user.Role);
        Assert.Equal(RegistrationSource.Bootstrap, user.Registration.Source);
    }

    [Fact]
    public void RegisterWritesTheFirstHistoryEntryWithoutActor()
    {
        var user = AUser.Registered(_time);

        var entry = Assert.Single(user.History);
        Assert.Equal(HistoryAction.Registered, entry.Action);
        Assert.Null(entry.ActorId);
        Assert.Equal(user.Id, entry.UserId);
        Assert.Equal("Password", entry.Details);
    }

    [Fact]
    public void EmptyIdIsAProgrammingError() =>
        Assert.Throws<ArgumentException>(() =>
            User.Register(Guid.Empty, A.Email(), ServerRole.User, A.Profile(), AUser.Registration(), _time));

    [Fact]
    public void D8_RegistrationKeepsInviterAndInvitedAddressCleaned()
    {
        var registration = Registration.Create(RegistrationSource.Google, A.Now, true, " Mama@Example.com ",
            "adam.invited@example.com", "homeapps-web", new string('x', 600), "pl-PL,pl;q=0.9");

        Assert.True(registration.ViaReferral);
        Assert.Equal("mama@example.com", registration.InvitedByEmail);
        Assert.Equal("adam.invited@example.com", registration.InvitedEmail);
        Assert.Equal(Registration.UserAgentMaxLength, registration.UserAgent!.Length);
    }
}
