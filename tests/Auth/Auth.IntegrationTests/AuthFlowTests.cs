using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HomeApps.Contracts.Accounts;
using HomeApps.Contracts.Auth;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth.IntegrationTests;

public sealed class AuthFlowTests(AuthHost auth) : IClassFixture<AuthHost>
{
    [Fact]
    public async Task Sign_up_reaches_accounts_through_rabbitmq()
    {
        using var client = auth.CreateClient();
        var email = AuthHost.Unique("signup");

        var user = await RegisterAsync(client, email);

        var registered = await AuthHost.EventuallyAsync(
            () => Task.FromResult(auth.Registered.FirstOrDefault(r => r.Email == email)), "IdentityRegistered");
        Assert.Equal(user.Id, registered.UserId);
        Assert.Equal(RegistrationSource.Password, registered.Source);
        Assert.False(registered.ViaReferral);
    }

    [Fact]
    public async Task Invitation_email_link_creates_an_account_that_remembers_the_inviter()
    {
        using var client = auth.CreateClient();
        var inviterEmail = AuthHost.Unique("inviter");
        var inviter = await RegisterAsync(client, inviterEmail);
        var inviteeEmail = AuthHost.Unique("invitee");

        using var invite = await client.SendAsync(Authorized(HttpMethod.Post, "/auth/invitations", inviter.AccessToken,
            JsonContent.Create(new { email = inviteeEmail })));
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);

        var link = await auth.Mail.LinkSentToAsync(inviteeEmail);
        var token = link.AbsolutePath.Split('/').Last();
        using var accepted = await client.PostAsJsonAsync($"/auth/invitations/{token}/accept", new
        {
            password = AuthHost.Password,
            displayName = "Ola",
            clientId = AuthHost.NativeClient,
        });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var registered = await AuthHost.EventuallyAsync(
            () => Task.FromResult(auth.Registered.FirstOrDefault(r => r.Email == inviteeEmail)), "IdentityRegistered");
        Assert.True(registered.ViaReferral);
        Assert.Equal(inviterEmail, registered.InvitedByEmail);
        Assert.Equal(inviteeEmail, registered.InvitedEmail);
    }

    [Fact]
    public async Task Password_reset_from_the_email_ends_old_sessions()
    {
        using var client = auth.CreateClient();
        var email = AuthHost.Unique("forgot");
        var user = await RegisterAsync(client, email);

        using var forgot = await client.PostAsJsonAsync("/auth/password/forgot", new { email });
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);
        var link = await auth.Mail.LinkSentToAsync(email);
        var token = QueryHelpers.ParseQuery(link.Query)["token"].ToString();

        const string newPassword = "a brand new passphrase";
        using var reset = await client.PostAsJsonAsync("/auth/password/reset", new { token, password = newPassword });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        using var oldSession = await client.PostAsJsonAsync("/auth/refresh", new { clientId = AuthHost.NativeClient, refreshToken = user.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
        using var login = await client.PostAsJsonAsync("/auth/login", new { email, password = newPassword, clientId = AuthHost.NativeClient });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Accounts_disabling_and_enabling_drives_sign_in()
    {
        using var client = auth.CreateClient();
        var email = AuthHost.Unique("disabled");
        var user = await RegisterAsync(client, email);
        var login = new { email, password = AuthHost.Password, clientId = AuthHost.NativeClient };

        await auth.PublishFromAccountsAsync(new UserDisabled(user.Id, BySelf: true));
        var ticket = await AuthHost.EventuallyAsync(async () =>
        {
            using var response = await client.PostAsJsonAsync("/auth/login", login);
            return response.StatusCode == HttpStatusCode.Forbidden
                ? (await response.ReadJsonAsync()).GetProperty("ticket").GetString()
                : null;
        }, "UserDisabled to reach Auth");

        using var pending = await client.PostAsJsonAsync("/auth/reactivation/complete", new { ticket, clientId = AuthHost.NativeClient });
        Assert.Equal(HttpStatusCode.Conflict, pending.StatusCode);

        await auth.PublishFromAccountsAsync(new UserEnabled(user.Id));
        await AuthHost.EventuallyAsync(async () =>
        {
            using var response = await client.PostAsJsonAsync("/auth/reactivation/complete", new { ticket, clientId = AuthHost.NativeClient });
            return response.StatusCode == HttpStatusCode.OK ? (object)true : null;
        }, "UserEnabled to reach Auth");
    }

    private sealed record User(Guid Id, string AccessToken, string RefreshToken);

    private static async Task<User> RegisterAsync(HttpClient client, string email)
    {
        using var registered = await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password = AuthHost.Password,
            displayName = "Adam",
            clientId = AuthHost.NativeClient,
        });
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        var tokens = await registered.ReadJsonAsync();
        var accessToken = tokens.GetProperty("accessToken").GetString()!;

        using var me = await client.SendAsync(Authorized(HttpMethod.Get, "/auth/me", accessToken));
        return new User((await me.ReadJsonAsync()).GetProperty("id").GetGuid(), accessToken, tokens.GetProperty("refreshToken").GetString()!);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string path, string accessToken, HttpContent? content = null) =>
        new(method, path)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
            Content = content,
        };
}
