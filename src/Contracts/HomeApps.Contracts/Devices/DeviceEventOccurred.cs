namespace HomeApps.Contracts.Devices;

/// <summary>
/// A household device published an event it declares in its descriptor (doorbell, motion, a tag seen at the door) and
/// Devices accepted and stored it (household-jarvis-devices.md 2.3). Plugins may listen to it.
/// </summary>
/// <param name="HouseholdId">The household the device belongs to.</param>
/// <param name="DeviceId">The device that sent it.</param>
/// <param name="Slug">The device's stable key, e.g. <c>fotel</c>.</param>
/// <param name="Name">The event's name from the descriptor, e.g. <c>button</c>.</param>
/// <param name="Payload">The event's data as a JSON document, or <c>null</c> when it has none.</param>
/// <param name="OccurredAt">When it happened, as the device said (never in the future).</param>
/// <param name="IsLocation">The event says where something is (a geo device, or declared as location): kept for a shorter time and shown to members only.</param>
public sealed record DeviceEventOccurred(
    Guid HouseholdId,
    Guid DeviceId,
    string Slug,
    string Name,
    string? Payload,
    DateTimeOffset OccurredAt,
    bool IsLocation) : IIntegrationEvent;
