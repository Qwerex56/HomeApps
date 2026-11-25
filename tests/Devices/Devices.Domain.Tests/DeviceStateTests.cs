using System.Text.Json;
using Devices.Domain.Devices;

namespace Devices.Domain.Tests;

public sealed class DeviceStateTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

    private static JsonElement Json(string json) => JsonElement.Parse(json);

    [Fact]
    public void New_values_replace_the_old_and_keep_the_others()
    {
        var state = DeviceState.For(Guid.NewGuid());
        state.ApplyValues(Json("""{ "heating": 1, "temperature": 20 }"""), Now);
        state.ApplyValues(Json("""{ "heating": 3 }"""), Now.AddMinutes(1));

        Assert.Equal(3, JsonElement.Parse(state.Values).GetProperty("heating").GetInt32());
        Assert.Equal(20, JsonElement.Parse(state.Values).GetProperty("temperature").GetInt32());
        Assert.Equal(Now.AddMinutes(1), state.ValuesAt);
    }

    [Fact]
    public void A_device_is_silent_after_two_and_a_half_periods_and_only_once()
    {
        var state = DeviceState.For(Guid.NewGuid());

        Assert.False(state.GoSilent(Now, Minute, Now.AddSeconds(150)));
        Assert.True(state.GoSilent(Now, Minute, Now.AddSeconds(151)));
        Assert.False(state.GoSilent(Now, Minute, Now.AddMinutes(10)));
        Assert.True(state.Silent);
    }

    [Fact]
    public void A_status_report_ends_the_silence_and_restarts_the_count()
    {
        var state = DeviceState.For(Guid.NewGuid());
        state.GoSilent(Now, Minute, Now.AddMinutes(5));

        var outcome = state.ApplyStatus(new StatusReport(null, null, -60, 10, "1.0.0", null), Now.AddMinutes(6));

        Assert.True(outcome.Returned);
        Assert.False(state.Silent);
        Assert.False(state.GoSilent(Now, Minute, Now.AddMinutes(8)));
        Assert.True(state.GoSilent(Now, Minute, Now.AddMinutes(9)));
    }

    [Fact]
    public void A_low_battery_is_reported_once_until_it_recovers()
    {
        var state = DeviceState.For(Guid.NewGuid());

        Assert.False(state.ApplyStatus(new StatusReport(50, null, null, null, null, null), Now).BatteryLow);
        Assert.True(state.ApplyStatus(new StatusReport(19, null, null, null, null, null), Now.AddMinutes(1)).BatteryLow);
        Assert.False(state.ApplyStatus(new StatusReport(15, null, null, null, null, null), Now.AddMinutes(2)).BatteryLow);
        Assert.False(state.ApplyStatus(new StatusReport(null, null, -50, null, null, null), Now.AddMinutes(3)).BatteryLow);
        Assert.False(state.ApplyStatus(new StatusReport(80, null, null, null, null, null), Now.AddMinutes(4)).BatteryLow);
        Assert.True(state.ApplyStatus(new StatusReport(10, null, null, null, null, null), Now.AddMinutes(5)).BatteryLow);
    }

    [Fact]
    public void Low_battery_re_arms_only_five_points_above_the_threshold()
    {
        var state = DeviceState.For(Guid.NewGuid());
        StatusOutcome Report(int battery, int minute) => state.ApplyStatus(new StatusReport(battery, null, null, null, null, null), Now.AddMinutes(minute));

        Assert.True(Report(19, 0).BatteryLow);
        Assert.False(Report(20, 1).BatteryLow);
        Assert.False(Report(19, 2).BatteryLow);
        Assert.False(Report(24, 3).BatteryLow);
        Assert.False(Report(19, 4).BatteryLow);
        Assert.False(Report(25, 5).BatteryLow);
        Assert.True(Report(19, 6).BatteryLow);
    }
}
