using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Auth.IntegrationTests;

public sealed class BootstrapHost : AuthHost
{
    public static readonly string OwnerEmail = Unique("owner");

    protected override string? BootstrapEmail => OwnerEmail;
}

public sealed class BootstrapTests(BootstrapHost auth) : IClassFixture<BootstrapHost>
{
    [Fact]
    public async Task Empty_server_invites_the_configured_owner()
    {
        using var client = auth.CreateClient();

        var link = await auth.Mail.LinkSentToAsync(BootstrapHost.OwnerEmail);
        using var accepted = await client.PostAsJsonAsync($"/auth/invitations/{link.AbsolutePath.Split('/').Last()}/accept", new
        {
            password = AuthHost.Password,
            displayName = "Gospodarz",
            clientId = AuthHost.NativeClient,
        });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        using var me = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", (await accepted.ReadJsonAsync()).GetProperty("accessToken").GetString());
        using var response = await client.SendAsync(me);
        Assert.Equal("Owner", (await response.ReadJsonAsync()).GetProperty("role").GetString());
    }
}
