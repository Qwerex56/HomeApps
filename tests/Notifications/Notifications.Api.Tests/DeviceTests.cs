using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Notifications.Api.Tests;

public sealed class DeviceTests(NotificationsApiFactory api) : IClassFixture<NotificationsApiFactory>
{
    [Fact]
    public async Task Without_a_token_is_401()
    {
        using var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/notifications/settings")).StatusCode);
    }

    [Fact]
    public async Task The_public_key_is_an_uncompressed_p256_point()
    {
        using var client = api.SignedIn(Guid.NewGuid());

        var key = (await client.GetFromJsonAsync<JsonElement>("/notifications/vapid-public-key")).GetProperty("publicKey").GetString()!;
        var bytes = Convert.FromBase64String(key.Replace('-', '+').Replace('_', '/').PadRight(88, '='));

        Assert.Equal(65, bytes.Length);
        Assert.Equal(0x04, bytes[0]);
    }

    [Fact]
    public async Task A_device_appears_in_the_settings_with_its_user_agent_and_can_be_removed()
    {
        var me = Guid.NewGuid();
        var endpoint = await api.DeviceAsync(me);
        using var client = api.SignedIn(me);

        var devices = (await client.GetFromJsonAsync<JsonElement>("/notifications/settings")).GetProperty("devices");
        var device = Assert.Single(devices.EnumerateArray());
        Assert.Equal(endpoint, device.GetProperty("endpoint").GetString());
        Assert.Contains("iPhone", device.GetProperty("userAgent").GetString(), StringComparison.Ordinal);

        var id = device.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/notifications/devices/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/notifications/devices/{id}")).StatusCode);
    }

    [Fact]
    public async Task Turning_off_on_this_device_forgets_it_by_endpoint()
    {
        var me = Guid.NewGuid();
        var endpoint = await api.DeviceAsync(me);
        using var client = api.SignedIn(me);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/notifications/devices?endpoint={Uri.EscapeDataString(endpoint)}")).StatusCode);

        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/notifications/settings")).GetProperty("devices").EnumerateArray());
    }

    [Fact]
    public async Task A_shared_browser_moves_to_the_person_signed_in_now()
    {
        var anna = Guid.NewGuid();
        var adam = Guid.NewGuid();
        var endpoint = await api.DeviceAsync(anna);

        await api.DeviceAsync(adam, endpoint);

        using var annaClient = api.SignedIn(anna);
        using var adamClient = api.SignedIn(adam);
        Assert.Empty((await annaClient.GetFromJsonAsync<JsonElement>("/notifications/settings")).GetProperty("devices").EnumerateArray());
        Assert.Single((await adamClient.GetFromJsonAsync<JsonElement>("/notifications/settings")).GetProperty("devices").EnumerateArray());
    }

    [Fact]
    public async Task A_broken_subscription_is_400()
    {
        using var client = api.SignedIn(Guid.NewGuid());

        using var plainHttp = await client.PutAsJsonAsync("/notifications/devices", NotificationsApiFactory.Subscription("http://push.example.com/a"));
        using var shortKey = await client.PutAsJsonAsync("/notifications/devices",
            new { endpoint = "https://push.example.com/a", keys = new { p256dh = "abc", auth = NotificationsApiFactory.Key(16) } });

        Assert.Equal("notifications.invalid_endpoint", await plainHttp.CodeAsync());
        Assert.Equal("notifications.invalid_keys", await shortKey.CodeAsync());
    }
}
