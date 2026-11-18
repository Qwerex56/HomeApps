namespace Devices.Domain.Devices;

/// <summary>
/// Something a household device published and Devices recorded (D9): kept for the household's retention, shorter for
/// <see cref="IsLocation"/> events. History is never overwritten, unlike <see cref="DeviceState"/>.
/// </summary>
public sealed class DeviceEvent
{
    public const int MaxNameLength = 60;

    private DeviceEvent() { }

    public Guid Id { get; private set; }

    public Guid DeviceId { get; private set; }

    public Guid HouseholdId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>The event's data as JSON, or <c>null</c>.</summary>
    public string? Data { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public bool IsLocation { get; private set; }

    public static DeviceEvent Record(Guid deviceId, Guid householdId, string name, string? data, DateTimeOffset occurredAt, bool isLocation) => new()
    {
        Id = Guid.CreateVersion7(),
        DeviceId = deviceId,
        HouseholdId = householdId,
        Name = name,
        Data = data,
        OccurredAt = occurredAt,
        IsLocation = isLocation,
    };
}
