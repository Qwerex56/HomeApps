using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using Auth.Infrastructure.Jobs;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Security;
using HomeApps.BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Tests;

public sealed class PersistenceTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private readonly FixedTime _time = new(A.Now);
    private readonly PasswordHasher _hasher = new();

    private async Task<Identity> Saved(string email)
    {
        var registration = A.Registered(_time, email);
        await using var db = database.CreateContext();
        new IdentityRepository(db).Add(registration.Identity);
        await db.SaveChangesAsync();
        return registration.Identity;
    }

    [Fact]
    public async Task Whole_identity_aggregate_round_trips()
    {
        var email = A.Unique("adam");
        var registration = A.Registered(_time, email);
        var identity = registration.Identity;
        identity.LinkExternalLogin(ExternalProvider.Google, "google-1", A.Email(email), false, _time);
        identity.SignInWithPassword("wrong password!", A.Client(), _hasher, _time);
        identity.RequestEmailChange(A.Password, A.Now, A.Email(A.Unique("new")), false, _hasher, _time);
        await using (var db = database.CreateContext())
        {
            new IdentityRepository(db).Add(identity);
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var loaded = await new IdentityRepository(read).GetByIdAsync(identity.Id, default);

        Assert.NotNull(loaded);
        Assert.Equal(identity.Email, loaded.Email);
        Assert.True(_hasher.Verify(loaded.PasswordHash!, A.Password));
        Assert.Equal(1, loaded.Lockout.FailedAttempts);
        Assert.Equal(EmailTokenPurpose.EmailChange, loaded.EmailToken!.Purpose);
        Assert.Equal(identity.EmailToken!.NewEmail, loaded.EmailToken.NewEmail);
        var login = Assert.Single(loaded.ExternalLogins);
        Assert.Equal("google-1", login.Subject);
        var session = Assert.Single(loaded.Sessions);
        Assert.Equal("Firefox", session.Client.UserAgent);
        Assert.Single(session.RefreshTokens);
        Assert.Equal(registration.RefreshToken.ExpiresAt, session.RefreshTokens[0].ExpiresAt);
    }

    [Fact]
    public async Task Lookups_find_the_identity_by_refresh_token_link_login_and_email()
    {
        var email = A.Unique("ola");
        var registration = A.Registered(_time, email);
        var identity = registration.Identity;
        identity.LinkExternalLogin(ExternalProvider.Google, $"sub-{identity.Id}", A.Email(email), false, _time);
        identity.RequestPasswordReset(_time);
        var resetToken = Assert.IsType<Domain.Identities.Events.PasswordResetRequested>(identity.DomainEvents[^1]).RawToken;
        await using (var db = database.CreateContext())
        {
            new IdentityRepository(db).Add(identity);
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var repository = new IdentityRepository(read);

        Assert.Equal(identity.Id, (await repository.GetByRefreshTokenHashAsync(OpaqueToken.HashOf(registration.RefreshToken.Raw), default))?.Id);
        Assert.Equal(identity.Id, (await repository.GetByEmailTokenHashAsync(OpaqueToken.HashOf(resetToken), default))?.Id);
        Assert.Equal(identity.Id, (await repository.GetByExternalLoginAsync(ExternalProvider.Google, $"sub-{identity.Id}", default))?.Id);
        Assert.Equal(identity.Id, (await repository.GetByEmailAsync(A.Email(email), default))?.Id);
        Assert.True(await repository.EmailExistsAsync(A.Email(email), default));
        Assert.True(await repository.ExternalLoginExistsAsync(ExternalProvider.Google, $"sub-{identity.Id}", default));
        Assert.Null(await repository.GetByRefreshTokenHashAsync(OpaqueToken.HashOf("unknown"), default));
    }

    [Fact]
    public async Task Database_rejects_a_second_identity_with_the_same_email()
    {
        var email = A.Unique("twice");
        await Saved(email);

        await Assert.ThrowsAsync<DbUpdateException>(() => Saved(email));
    }

    [Fact]
    public async Task Database_rejects_the_same_google_account_on_two_identities()
    {
        var subject = $"shared-{Guid.NewGuid():N}";
        foreach (var email in new[] { A.Unique("a"), A.Unique("b") })
        {
            var identity = A.Registered(_time, email).Identity;
            identity.LinkExternalLogin(ExternalProvider.Google, subject, A.Email(email), false, _time);
            await using var db = database.CreateContext();
            new IdentityRepository(db).Add(identity);
            if (email.StartsWith('a'))
            {
                await db.SaveChangesAsync();
            }
            else
            {
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            }
        }
    }

    [Fact]
    public async Task Two_refreshes_with_the_same_token_let_only_the_first_save()
    {
        var registration = A.Registered(_time, A.Unique("tabs"));
        await using (var db = database.CreateContext())
        {
            new IdentityRepository(db).Add(registration.Identity);
            await db.SaveChangesAsync();
        }

        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var tabA = await new IdentityRepository(first).GetByIdAsync(registration.Identity.Id, default);
        var tabB = await new IdentityRepository(second).GetByIdAsync(registration.Identity.Id, default);
        Assert.True(tabA!.Refresh(registration.RefreshToken.Raw, A.Client(), _time).IsSuccess);
        Assert.True(tabB!.Refresh(registration.RefreshToken.Raw, A.Client(), _time).IsSuccess);

        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => second.SaveChangesAsync());

        await using var read = database.CreateContext();
        var stored = await new IdentityRepository(read).GetByIdAsync(registration.Identity.Id, default);
        Assert.Single(stored!.ActiveSessions(_time.Now));
    }

    [Fact]
    public async Task Removing_an_identity_deletes_its_logins_sessions_and_tokens()
    {
        var registration = A.Registered(_time, A.Unique("gone"));
        await using (var db = database.CreateContext())
        {
            new IdentityRepository(db).Add(registration.Identity);
            await db.SaveChangesAsync();
        }

        await using (var db = database.CreateContext())
        {
            var repository = new IdentityRepository(db);
            repository.Remove((await repository.GetByIdAsync(registration.Identity.Id, default))!);
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        Assert.False(await read.Set<Session>().AnyAsync(s => s.IdentityId == registration.Identity.Id));
        Assert.False(await read.Set<RefreshToken>().AnyAsync(t => t.SessionId == registration.RefreshToken.SessionId));
    }

    [Fact]
    public async Task Invitations_round_trip_and_pending_is_derived_from_the_dates()
    {
        var inviter = await Saved(A.Unique("inviter"));
        var email = A.Email(A.Unique("guest"));
        var issued = Invitation.Create(new Inviter(inviter.Id, inviter.Email, true), email, false, false, _time).Value;
        await using (var db = database.CreateContext())
        {
            new InvitationRepository(db).Add(issued.Invitation);
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var repository = new InvitationRepository(read);
        var loaded = await repository.GetByTokenHashAsync(OpaqueToken.HashOf(issued.RawToken), default);

        Assert.Equal(issued.Invitation.Code, loaded!.Code);
        Assert.Equal(inviter.Email, loaded.InvitedByEmail);
        Assert.True(await repository.HasPendingForAsync(email, A.Now, default));
        Assert.False(await repository.HasPendingForAsync(email, issued.Invitation.ExpiresAt, default));
        Assert.Single(await repository.ListAsync(inviter.Id, default));
    }

    [Fact]
    public async Task Forgetting_the_invitee_clears_only_the_invited_email()
    {
        var bootstrap = Invitation.CreateBootstrap(A.Email(A.Unique("owner")), _time);
        await using (var db = database.CreateContext())
        {
            new InvitationRepository(db).Add(bootstrap.Invitation);
            await db.SaveChangesAsync();
            bootstrap.Invitation.ForgetInvitee();
            await db.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var loaded = await new InvitationRepository(read).GetByIdAsync(bootstrap.Invitation.Id, default);
        Assert.Null(loaded!.Email);
        Assert.Equal(InvitationKind.Bootstrap, loaded.Kind);
    }

    [Fact]
    public async Task Cleanup_deletes_only_dead_sessions_tokens_and_links()
    {
        var old = A.Registered(_time, A.Unique("old")).Identity;
        old.SignOutEverywhere(_time);
        old.RequestPasswordReset(_time);
        var live = A.Registered(_time, A.Unique("live")).Identity;
        await using (var db = database.CreateContext())
        {
            var repository = new IdentityRepository(db);
            repository.Add(old);
            repository.Add(live);
            await db.SaveChangesAsync();
        }

        await using (var db = database.CreateContext())
        {
            await AuthCleanup.RunAsync(db, A.Now + AuthCleanup.Retention + TimeSpan.FromMinutes(1), default);
        }

        await using var read = database.CreateContext();
        var repositoryAfter = new IdentityRepository(read);
        var oldAfter = await repositoryAfter.GetByIdAsync(old.Id, default);
        var liveAfter = await repositoryAfter.GetByIdAsync(live.Id, default);
        Assert.Empty(oldAfter!.Sessions);
        Assert.Null(oldAfter.EmailToken);
        Assert.Single(liveAfter!.Sessions);
    }
}
