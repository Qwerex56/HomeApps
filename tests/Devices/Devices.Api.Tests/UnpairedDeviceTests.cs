using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.Contracts.Households;

namespace Devices.Api.Tests;

/// <summary>H-4c item 4 (pinning): a device that was removed or got a new code cannot link again, even with a still-valid token.</summary>
public sealed class UnpairedDeviceTests(DevicesApiFactory api) : IClassFixture<DevicesApiFactory>
{
    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _admin = Guid.NewGuid();

    private async Task<(Guid Id, string Code)> AddAndPairAsync(HttpClient admin, string name)
    {
        var added = await (await admin.PostAsJsonAsync($"/devices/households/{_household}", new { name })).Content.ReadFromJsonAsync<JsonElement>();
        using var anonymous = api.CreateClient();
        using var paired = await anonymous.PostAsJsonAsync("/devices/pair", new { code = added.GetProperty("code").GetString() });
        Assert.True(paired.IsSuccessStatusCode);
        return (added.GetProperty("deviceId").GetGuid(), added.GetProperty("code").GetString()!);
    }

    [Fact]
    public async Task A_removed_satellite_with_a_valid_token_is_told_devices_disabled()
    {
        api.Members.Add(_admin, _household, HouseholdRole.Admin);
        using var admin = api.CreateAuthenticatedClient(_admin);
        var (id, _) = await AddAndPairAsync(admin, "garaz");

        await admin.DeleteAsync($"/devices/{id}");
        await using var device = await FakeDevice.ConnectAsync(api, id, "garaz");

        Assert.Equal("error", device.First.GetProperty("type").GetString());
        Assert.Equal("devices.disabled", device.First.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_satellite_given_a_new_code_cannot_link_until_it_pairs_again()
    {
        api.Members.Add(_admin, _household, HouseholdRole.Admin);
        using var admin = api.CreateAuthenticatedClient(_admin);
        var (id, _) = await AddAndPairAsync(admin, "kuchnia");

        await admin.PostAsync($"/devices/{id}/pairing-code", null);
        await using var device = await FakeDevice.ConnectAsync(api, id, "kuchnia");

        Assert.Equal("error", device.First.GetProperty("type").GetString());
        Assert.Equal("devices.not_paired", device.First.GetProperty("code").GetString());
    }
}
