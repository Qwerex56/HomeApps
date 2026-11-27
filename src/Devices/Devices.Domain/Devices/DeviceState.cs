using System.Text.Json;
using System.Text.Json.Nodes;

namespace Devices.Domain.Devices;

/// <summary>
/// What Devices last knows about a household device (household-jarvis-devices.md 2.3): the latest value of every control
/// and sensor with the time it was read, and the last status report (heartbeat) with its health flags. A new value
/// replaces the old one; values the device does not mention stay.
/// </summary>
public sealed class DeviceState
{
    /// <summary>A device is silent after this many heartbeat periods without a status report.</summary>
    public const double SilentAfterPeriods = 2.5;

    /// <summary>The battery percentage below which admins are told (once per drop).</summary>
    public const int DefaultLowBatteryPercent = 20;

    private DeviceState() { }

    public Guid DeviceId { get; private set; }

    /// <summary>A JSON object: control and sensor names with their last values.</summary>
    public string Values { get; private set; } = "{}";

    public DateTimeOffset? ValuesAt { get; private set; }

    public DateTimeOffset? LastStatusAt { get; private set; }

    public double? Battery { get; private set; }

    public double? Voltage { get; private set; }

    public int? Rssi { get; private set; }

    public long? Uptime { get; private set; }

    public string? Firmware { get; private set; }

    public string? Error { get; private set; }

    /// <summary>No status report for <see cref="SilentAfterPeriods"/> periods; ends with the next report. Not the same as offline.</summary>
    public bool Silent { get; private set; }

    /// <summary>The battery is below the threshold and admins were told; ends when it recovers.</summary>
    public bool LowBattery { get; private set; }

    /// <summary>
    /// A JSON object of control values a sleepy device has not applied yet (last value wins, no queue), or <c>null</c>.
    /// Sent with every report and cleared only when the device reports the value in a <c>state</c> frame.
    /// </summary>
    public string? PendingSettings { get; private set; }

    public static DeviceState For(Guid deviceId) => new() { DeviceId = deviceId };

    /// <summary>
    /// The device reported values; they replace the same names and keep the others. Returns the values that differ
    /// from the stored ones as a JSON object, or <c>null</c> when nothing changed (the time is still refreshed).
    /// </summary>
    public string? ApplyValues(JsonElement values, DateTimeOffset now)
    {
        var current = JsonNode.Parse(Values)!.AsObject();
        var changed = new JsonObject();
        foreach (var value in values.EnumerateObject())
        {
            var next = JsonNode.Parse(value.Value.GetRawText());
            if (!current.TryGetPropertyValue(value.Name, out var before) || !JsonNode.DeepEquals(before, next))
            {
                changed[value.Name] = next?.DeepClone();
            }

            current[value.Name] = next;
        }

        Values = current.ToJsonString();
        ValuesAt = now;
        return changed.Count == 0 ? null : changed.ToJsonString();
    }

    /// <summary>A member set a control of a sleepy device: it replaces an earlier pending value of the same control.</summary>
    public void SetPendingSetting(string control, JsonElement value)
    {
        var pending = PendingSettings is null ? new JsonObject() : JsonNode.Parse(PendingSettings)!.AsObject();
        pending[control] = JsonNode.Parse(value.GetRawText());
        PendingSettings = pending.ToJsonString();
    }

    /// <summary>A copy of the pending settings for the report answer; nothing is cleared here.</summary>
    public JsonElement? GetPendingSettingsForReport() =>
        PendingSettings is null ? null : JsonElement.Parse(PendingSettings);

    /// <summary>
    /// A <c>state</c> frame arrived: a pending setting whose value the device now reports is applied and goes away.
    /// A different value (the device reported before it got the setting) keeps it pending. Returns whether anything went.
    /// </summary>
    public bool ClearPendingSettingOnState(JsonElement reportedValues)
    {
        if (PendingSettings is null)
        {
            return false;
        }

        var pending = JsonNode.Parse(PendingSettings)!.AsObject();
        var cleared = false;
        foreach (var reported in reportedValues.EnumerateObject())
        {
            if (pending.TryGetPropertyValue(reported.Name, out var wanted) && JsonNode.DeepEquals(wanted, JsonNode.Parse(reported.Value.GetRawText())))
            {
                pending.Remove(reported.Name);
                cleared = true;
            }
        }

        PendingSettings = pending.Count == 0 ? null : pending.ToJsonString();
        return cleared;
    }

    private const int LowBatteryHysteresis = 5;

    /// <summary>A heartbeat arrived: remember it, end silence, and tell whether the battery just became low.</summary>
    public StatusOutcome ApplyStatus(StatusReport report, DateTimeOffset now, int lowBatteryPercent = DefaultLowBatteryPercent)
    {
        ArgumentNullException.ThrowIfNull(report);
        var returned = Silent;
        Silent = false;
        LastStatusAt = now;
        Battery = report.Battery ?? Battery;
        Voltage = report.Voltage ?? Voltage;
        Rssi = report.Rssi ?? Rssi;
        Uptime = report.Uptime ?? Uptime;
        Firmware = report.Firmware ?? Firmware;
        Error = report.Error;

        var batteryLow = false;
        if (report.Battery is { } battery)
        {
            if (battery < lowBatteryPercent)
            {
                batteryLow = !LowBattery;
                LowBattery = true;
            }
            else if (battery >= lowBatteryPercent + LowBatteryHysteresis)
            {
                LowBattery = false;
            }
        }

        return new StatusOutcome(returned, batteryLow);
    }

    /// <summary>
    /// The heartbeat check: <paramref name="reference"/> is when the device was last heard of (its last report, or when
    /// it was paired or the check started). True exactly once, when it becomes silent.
    /// </summary>
    public bool GoSilent(DateTimeOffset reference, TimeSpan period, DateTimeOffset now)
    {
        var lastHeard = LastStatusAt is { } last && last > reference ? last : reference;
        if (Silent || now - lastHeard <= period * SilentAfterPeriods)
        {
            return false;
        }

        Silent = true;
        return true;
    }
}
