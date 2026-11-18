using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.Contracts.Households;

namespace Devices.Api.Tests;

/// <summary>A household's satellites (jarvis-satellites.md 3a, S-1a): admins add them and get pairing codes, members see them.</summary>
public sealed class HouseholdDevicesTests(DevicesApiFactory api) : IClassFixture<DevicesApiFactory>
{
    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _admin = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();

    [Fact]
    public async Task An_admin_adds_a_satellite_and_gets_a_one_time_code_and_members_see_it_unpaired()
    {
        Join();
        using var admin = api.CreateAuthenticatedClient(_admin);

        var added = await AddAsync(admin, "salon");

        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var code = (await added.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()!;
        Assert.Matches("^[A-HJ-NP-Z2-9]{8}$", code);
        using var member = api.CreateAuthenticatedClient(_member);
        var listed = Assert.Single((await member.GetFromJsonAsync<JsonElement>($"/devices/households/{_household}")).EnumerateArray());
        Assert.Equal("salon", listed.GetProperty("name").GetString());
        Assert.Equal("Satellite", listed.GetProperty("kind").GetString());
        Assert.False(listed.GetProperty("paired").GetBoolean());
        Assert.Empty((await admin.GetFromJsonAsync<JsonElement>("/devices")).EnumerateArray());
    }

    [Fact]
    public async Task Only_admins_change_household_devices_and_outsiders_learn_nothing()
    {
        Join();
        using var admin = api.CreateAuthenticatedClient(_admin);
        var id = (await (await AddAsync(admin, "kuchnia")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("deviceId").GetGuid();
        using var member = api.CreateAuthenticatedClient(_member);
        using var outsider = api.CreateAuthenticatedClient(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, (await AddAsync(member, "lazienka")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.DeleteAsync($"/devices/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AddAsync(outsider, "garaz")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/devices/households/{_household}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsync($"/devices/{id}/pairing-code", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/devices/{id}")).StatusCode);
    }

    [Fact]
    public async Task Names_are_unique_in_the_household_and_a_new_code_replaces_the_old_one()
    {
        Join();
        using var admin = api.CreateAuthenticatedClient(_admin);
        var first = await (await AddAsync(admin, "salon")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Conflict, (await AddAsync(admin, "salon")).StatusCode);
        var renewed = await admin.PostAsync($"/devices/{first.GetProperty("deviceId").GetGuid()}/pairing-code", null);
        var second = await renewed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        Assert.NotEqual(first.GetProperty("code").GetString(), second.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_satellite_pairs_once_with_its_code_and_connects_as_itself_under_its_household_name()
    {
        Join();
        using var admin = api.CreateAuthenticatedClient(_admin);
        var added = await (await AddAsync(admin, "salon")).Content.ReadFromJsonAsync<JsonElement>();
        var code = added.GetProperty("code").GetString()!;
        using var anonymous = api.CreateClient();

        var paired = await anonymous.PostAsJsonAsync("/devices/pair", new { code = code.ToLowerInvariant().Insert(4, "-") });
        var again = await anonymous.PostAsJsonAsync("/devices/pair", new { code });

        Assert.Equal(HttpStatusCode.OK, paired.StatusCode);
        var satellite = await paired.Content.ReadFromJsonAsync<JsonElement>();
        var id = satellite.GetProperty("deviceId").GetGuid();
        Assert.Equal(_household, satellite.GetProperty("householdId").GetGuid());
        Assert.Equal($"secret-of-{id}", satellite.GetProperty("secret").GetString());
        Assert.Contains((id, _household, "salon"), api.Identities.Paired);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);

        await using var device = await FakeDevice.ConnectAsync(api, id, "jakas-inna-nazwa", FakeDevice.Action("say"));
        Assert.Equal("ready", device.First.GetProperty("type").GetString());
        var listed = Assert.Single((await admin.GetFromJsonAsync<JsonElement>($"/devices/households/{_household}")).EnumerateArray());
        Assert.Equal("salon", listed.GetProperty("name").GetString());
        Assert.True(listed.GetProperty("paired").GetBoolean());
    }

    [Fact]
    public async Task A_removed_satellite_is_unpaired_and_cannot_connect()
    {
        Join();
        using var admin = api.CreateAuthenticatedClient(_admin);
        var added = await (await AddAsync(admin, "garaz")).Content.ReadFromJsonAsync<JsonElement>();
        var id = added.GetProperty("deviceId").GetGuid();
        await api.CreateClient().PostAsJsonAsync("/devices/pair", new { code = added.GetProperty("code").GetString() });

        await admin.DeleteAsync($"/devices/{id}");
        await using var device = await FakeDevice.ConnectAsync(api, id, "garaz");

        Assert.Equal("error", device.First.GetProperty("type").GetString());
    }

    private void Join()
    {
        api.Members.Add(_admin, _household, HouseholdRole.Admin);
        api.Members.Add(_member, _household, HouseholdRole.Member);
    }

    private Task<HttpResponseMessage> AddAsync(HttpClient client, string name) =>
        client.PostAsJsonAsync($"/devices/households/{_household}", new { name });
}
