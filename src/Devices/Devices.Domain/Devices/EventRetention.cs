namespace Devices.Domain.Devices;

/// <summary>How many days a household keeps device events (D9): <paramref name="EventDays"/>, and <paramref name="LocationEventDays"/> for those with a location.</summary>
public sealed record EventRetention(int EventDays, int LocationEventDays)
{
    /// <summary>Households' defaults, used for a household that no longer answers (archived).</summary>
    public static readonly EventRetention Default = new(30, 7);

    /// <summary>The longest retention a household can set (a year and a day); a device's own date older than this is not believed.</summary>
    public static readonly TimeSpan MaxEventAge = TimeSpan.FromDays(366);

    /// <summary>Events older than this go: location events by their shorter time, never kept longer than the others.</summary>
    public DateTimeOffset CutoffFor(bool isLocation, DateTimeOffset now) =>
        now.AddDays(-(isLocation ? Math.Min(LocationEventDays, EventDays) : EventDays));
}
