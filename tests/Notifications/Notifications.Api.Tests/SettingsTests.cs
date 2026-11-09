using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Notifications.Api.Tests;

public sealed class SettingsTests(NotificationsApiFactory api) : IClassFixture<NotificationsApiFactory>
{
    [Fact]
    public async Task Someone_who_never_set_anything_gets_the_defaults()
    {
        using var client = api.SignedIn(Guid.NewGuid());

        var settings = await client.GetFromJsonAsync<JsonElement>("/notifications/settings");

        Assert.True(settings.GetProperty("showPreview").GetBoolean());
        Assert.Equal(JsonValueKind.Null, settings.GetProperty("quietHours").ValueKind);
        Assert.Empty(settings.GetProperty("mutes").EnumerateArray());
    }

    [Fact]
    public async Task Preview_and_quiet_hours_are_kept()
    {
        using var client = api.SignedIn(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/notifications/settings",
            new { showPreview = false, quietHours = new { from = "22:00", to = "07:00", timeZone = "Europe/Warsaw" } })).StatusCode);

        var settings = await client.GetFromJsonAsync<JsonElement>("/notifications/settings");
        Assert.False(settings.GetProperty("showPreview").GetBoolean());
        Assert.Equal("22:00", settings.GetProperty("quietHours").GetProperty("from").GetString());
        Assert.Equal("Europe/Warsaw", settings.GetProperty("quietHours").GetProperty("timeZone").GetString());

        await client.PutAsJsonAsync("/notifications/settings", new { showPreview = true, quietHours = (object?)null });
        Assert.Equal(JsonValueKind.Null,
            (await client.GetFromJsonAsync<JsonElement>("/notifications/settings")).GetProperty("quietHours").ValueKind);
    }

    [Fact]
    public async Task Bad_quiet_hours_are_400()
    {
        using var client = api.SignedIn(Guid.NewGuid());

        using var badTime = await client.PutAsJsonAsync("/notifications/settings",
            new { showPreview = true, quietHours = new { from = "25:00", to = "07:00", timeZone = "UTC" } });
        using var badZone = await client.PutAsJsonAsync("/notifications/settings",
            new { showPreview = true, quietHours = new { from = "22:00", to = "07:00", timeZone = "Mars/Olympus" } });

        Assert.Equal(HttpStatusCode.BadRequest, badTime.StatusCode);
        Assert.Equal("notifications.unknown_time_zone", await badZone.CodeAsync());
    }

    [Fact]
    public async Task Mutes_of_a_household_and_of_a_plugin_in_it()
    {
        var household = Guid.NewGuid();
        using var client = api.SignedIn(Guid.NewGuid());

        await client.PutAsJsonAsync("/notifications/mutes", new { householdId = household, source = (string?)null });
        await client.PutAsJsonAsync("/notifications/mutes", new { householdId = household, source = "chat" });
        await client.PutAsJsonAsync("/notifications/mutes", new { householdId = household, source = "chat" });

        var mutes = (await client.GetFromJsonAsync<JsonElement>("/notifications/settings")).GetProperty("mutes").EnumerateArray().ToList();
        Assert.Equal(2, mutes.Count);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/notifications/mutes?householdId={household}&source=chat")).StatusCode);
        var left = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/notifications/settings")).GetProperty("mutes").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, left.GetProperty("source").ValueKind);
    }
}
