using System.Net;
using System.Net.Http.Json;

namespace Auth.Api.Tests;

public sealed class DeviceUnpairedTests(AuthApiFactory api) : IClassFixture<AuthApiFactory>
{
    private const string DevicesClient = "devices-service";
    private const string DevicesSecret = "development-only-devices-secret-0123456789";

    [Fact]
    public async Task The_holder_of_the_right_secret_of_an_unpaired_device_gets_401_device_unpaired_and_the_old_secret_invalid_credentials()
    {
        using var client = api.CreateClient();
        var deviceId = Guid.NewGuid();

        var first = await PairAsync(client, deviceId);
        using (var active = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = first }))
        {
            Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        }

        await api.SendAsync(new Auth.Application.DevicesSync.UnpairDevice.UnpairDeviceCommand(deviceId));

        using var unpaired = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = first });
        using var wrong = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = "guess" });
        var second = await PairAsync(client, deviceId);
        using var old = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = first });
        using var fresh = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = second });

        Assert.Equal(HttpStatusCode.Unauthorized, unpaired.StatusCode);
        Assert.Equal("devices.device_unpaired", (await unpaired.ReadJsonAsync()).GetProperty("code").GetString());
        Assert.Equal("devices.invalid_credentials", (await wrong.ReadJsonAsync()).GetProperty("code").GetString());
        Assert.Equal("devices.invalid_credentials", (await old.ReadJsonAsync()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
    }

    private static async Task<string> PairAsync(HttpClient client, Guid deviceId)
    {
        using var paired = await client.PostAsJsonAsync("/internal/devices",
            new { clientId = DevicesClient, clientSecret = DevicesSecret, deviceId, householdId = Guid.NewGuid(), name = "salon" });
        return (await paired.ReadJsonAsync()).GetProperty("secret").GetString()!;
    }
}
