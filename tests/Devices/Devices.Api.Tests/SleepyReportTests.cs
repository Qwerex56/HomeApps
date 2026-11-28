using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;
using HomeApps.Contracts.Households;
using Microsoft.Extensions.DependencyInjection;

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
        var added = await (await member.PostAsJsonAsync($"/devices/v1/households/{household}", new { name = "sleeper" })).Content.ReadFromJsonAsync<JsonElement>();
        using var anonymous = api.CreateClient();
        var paired = await anonymous.PostAsJsonAsync("/devices/v1/pair", new { code = added.GetProperty("code").GetString(), descriptor = JsonElement.Parse(Descriptor) });
        var id = (await paired.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("deviceId").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsJsonAsync($"/devices/v1/{id}/set", new { control = "level", value = 3 })).StatusCode);

        Assert.Equal(3, (await PendingAsync(member, household, id)).GetProperty("level").GetInt32());

        using var device = api.CreateClient();
        device.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateDeviceToken(id, household));
        var first = await (await device.PostAsJsonAsync("/devices/v1/report", new[] { new { type = "status" } })).Content.ReadFromJsonAsync<JsonElement>();
        var again = await (await device.PostAsJsonAsync("/devices/v1/report", new[] { new { type = "status" } })).Content.ReadFromJsonAsync<JsonElement>();
        var applied = await (await device.PostAsJsonAsync("/devices/v1/report", new object[] { new { type = "state", values = new { level = 3 } } })).Content.ReadFromJsonAsync<JsonElement>();
        var after = await (await device.PostAsJsonAsync("/devices/v1/report", new[] { new { type = "status" } })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Null, (await PendingAsync(member, household, id)).ValueKind);
        Assert.True(first.GetProperty("results")[0].GetProperty("ok").GetBoolean());
        Assert.Equal(3, first.GetProperty("settings").GetProperty("level").GetInt32());
        Assert.Equal(3, again.GetProperty("settings").GetProperty("level").GetInt32());
        Assert.Equal(JsonValueKind.Null, applied.GetProperty("settings").ValueKind);
        Assert.Equal(JsonValueKind.Null, after.GetProperty("settings").ValueKind);
    }

    /// <summary>pendingSettings from both the household list (?state=true) and the device's state; they must agree.</summary>
    private static async Task<JsonElement> PendingAsync(HttpClient member, Guid household, Guid id)
    {
        var state = (await member.GetFromJsonAsync<JsonElement>($"/devices/v1/{id}/state")).GetProperty("pendingSettings");
        var listed = (await member.GetFromJsonAsync<JsonElement>($"/devices/v1/households/{household}?state=true"))
            .EnumerateArray().Single(d => d.GetProperty("id").GetGuid() == id).GetProperty("state").GetProperty("pendingSettings");
        Assert.Equal(state.GetRawText(), listed.GetRawText());
        return state;
    }

    [Fact]
    public async Task A_state_row_changed_by_a_set_after_it_was_loaded_is_not_overwritten()
    {
        var household = Guid.NewGuid();
        var admin = Guid.NewGuid();
        api.Members.Add(admin, household, HouseholdRole.Admin);
        using var member = api.CreateAuthenticatedClient(admin);
        var added = await (await member.PostAsJsonAsync($"/devices/v1/households/{household}", new { name = "racer" })).Content.ReadFromJsonAsync<JsonElement>();
        var device = added.GetProperty("deviceId").GetGuid();
        await using (var seed = Scope())
        {
            var state = Devices.Domain.Devices.DeviceState.For(device);
            seed.Context.Add(state);
            await seed.Context.SaveChangesAsync();
        }

        await using var stale = Scope();
        var loaded = await stale.Context.Set<Devices.Domain.Devices.DeviceState>().FindAsync(device);
        await using (var newer = Scope())
        {
            var fresh = await newer.Context.Set<Devices.Domain.Devices.DeviceState>().FindAsync(device);
            fresh!.SetPendingSetting("level", JsonElement.Parse("3"));
            await newer.Context.SaveChangesAsync();
        }

        loaded!.ClearPendingSettingOnState(JsonElement.Parse("""{ "level": 1 }"""));
        loaded.ApplyValues(JsonElement.Parse("""{ "level": 1 }"""), DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<HomeApps.BuildingBlocks.Application.Abstractions.ConcurrencyConflictException>(() => stale.Context.SaveChangesAsync());
    }

    private ScopedContext Scope() => new(api.Services.CreateAsyncScope());

    private sealed class ScopedContext(AsyncServiceScope scope) : IAsyncDisposable
    {
        public Devices.Infrastructure.Persistence.DevicesDbContext Context { get; } = scope.ServiceProvider.GetRequiredService<Devices.Infrastructure.Persistence.DevicesDbContext>();

        public ValueTask DisposeAsync() => scope.DisposeAsync();
    }
}
