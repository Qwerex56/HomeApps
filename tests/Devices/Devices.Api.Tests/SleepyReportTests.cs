using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;
using HomeApps.Contracts.Households;

namespace Devices.Api.Tests;

/// <summary>jarvis-desktop-rework 2.8: <c>POST /devices/v1/report</c> answers <c>{ results, settings }</c>; a pending setting survives until the device reports it.</summary>
public sealed class SleepyReportTests(DevicesApiFactory api) : IClassFixture<DevicesApiFactory>
{
    private const string Descriptor = """
        { "format": "homeapps.device/1", "model": "a/sleepy", "version": "1.0.0", "purpose": "smart_home", "sleepy": true,
          "controls": [ { "name": "level", "value": { "type": "integer", "minimum": 0, "maximum": 3 } } ] }
        """;

    [Fact]
    public async Task A_report_answers_results_and_the_pending_settings_until_the_device_reports_the_value()
    {
        var household = Guid.NewGuid();
        var admin = Guid.NewGuid();
        api.Members.Add(admin, household, HouseholdRole.Admin);
        using var member = api.CreateAuthenticatedClient(admin);
        var added = await (await member.PostAsJsonAsync($"/devices/households/{household}", new { name = "sleeper" })).Content.ReadFromJsonAsync<JsonElement>();
        using var anonymous = api.CreateClient();
        var paired = await anonymous.PostAsJsonAsync("/devices/pair", new { code = added.GetProperty("code").GetString(), descriptor = JsonDocument.Parse(Descriptor).RootElement });
        var id = (await paired.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("deviceId").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsJsonAsync($"/devices/{id}/set", new { control = "level", value = 3 })).StatusCode);

        using var device = api.CreateClient();
        device.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateDeviceToken(id, household));
        var first = await (await device.PostAsJsonAsync("/devices/v1/report", new[] { new { type = "status" } })).Content.ReadFromJsonAsync<JsonElement>();
        var again = await (await device.PostAsJsonAsync("/devices/v1/report", new[] { new { type = "status" } })).Content.ReadFromJsonAsync<JsonElement>();
        var applied = await (await device.PostAsJsonAsync("/devices/v1/report", new object[] { new { type = "state", values = new { level = 3 } } })).Content.ReadFromJsonAsync<JsonElement>();
        var after = await (await device.PostAsJsonAsync("/devices/v1/report", new[] { new { type = "status" } })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(first.GetProperty("results")[0].GetProperty("ok").GetBoolean());
        Assert.Equal(3, first.GetProperty("settings").GetProperty("level").GetInt32());
        Assert.Equal(3, again.GetProperty("settings").GetProperty("level").GetInt32());
        Assert.Equal(JsonValueKind.Null, applied.GetProperty("settings").ValueKind);
        Assert.Equal(JsonValueKind.Null, after.GetProperty("settings").ValueKind);
    }
}
