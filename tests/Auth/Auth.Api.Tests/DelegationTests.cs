using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Auth.Api.Tests;

/// <summary>jarvis-v1.md, section 3: JARVIS's delegated token reads like the person and changes nothing in Auth.</summary>
public sealed class DelegationTests(AuthApiFactory api) : IClassFixture<AuthApiFactory>
{
    // The service client from appsettings.Development.json.
    private const string ServiceClient = "jarvis-service";
    private const string ServiceSecret = "development-only-jarvis-secret-0123456789";

    [Fact]
    public async Task The_service_acts_for_a_person_who_keeps_every_change_to_themselves()
    {
        using var client = api.CreateClient();
        var userId = await RegisteredUserAsync(client);

        using var delegation = await client.PostAsJsonAsync("/internal/delegation",
            new { clientId = ServiceClient, clientSecret = ServiceSecret, userId, channel = "test", reference = "1" });
        Assert.Equal(HttpStatusCode.OK, delegation.StatusCode);
        var token = (await delegation.ReadJsonAsync()).GetProperty("accessToken").GetString();

        using var me = await Send(client, HttpMethod.Get, "/auth/me", token!);
        using var password = await Send(client, HttpMethod.Put, "/auth/me/password", token!,
            new { currentPassword = AuthApiFactory.Password, newPassword = "another long password" });
        using var invitation = await Send(client, HttpMethod.Post, "/auth/invitations", token!, new { email = "x@example.com" });

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(userId, (await me.ReadJsonAsync()).GetProperty("id").GetGuid());
        foreach (var refused in new[] { password, invitation })
        {
            Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
            Assert.Equal("assistant.not_allowed", (await refused.ReadJsonAsync()).GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task A_wrong_secret_gets_nothing()
    {
        using var client = api.CreateClient();
        var userId = await RegisteredUserAsync(client);

        using var response = await client.PostAsJsonAsync("/internal/delegation",
            new { clientId = ServiceClient, clientSecret = "guess-guess-guess-guess-guess-guess", userId, channel = "test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("delegation.client_invalid", (await response.ReadJsonAsync()).GetProperty("code").GetString());
    }

    private static async Task<Guid> RegisteredUserAsync(HttpClient client)
    {
        using var registered = await AuthApiFactory.RegisterAsync(client, AuthApiFactory.Unique("delegation"), AuthApiFactory.NativeClient);
        var token = (await registered.ReadJsonAsync()).GetProperty("accessToken").GetString();
        using var me = await Send(client, HttpMethod.Get, "/auth/me", token!);
        return (await me.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }
}
