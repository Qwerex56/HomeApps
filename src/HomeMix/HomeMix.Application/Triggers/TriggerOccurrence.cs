using System.Text.Json;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Triggers;

/// <summary>A device changed state or published an event, as the trigger dispatcher sees it.</summary>
/// <param name="Changed">State: only the values that changed.</param>
/// <param name="EventName">Event: its name.</param>
/// <param name="Payload">Event: its data, or <c>null</c>.</param>
/// <param name="ChangedBy">State: the source Devices stamped on a change an add-on caused, see <see cref="TriggerSource"/>.</param>
public sealed record TriggerOccurrence(
    Guid HouseholdId,
    DeviceCandidate Device,
    string Slug,
    TriggerKind Kind,
    JsonElement? Changed,
    string? EventName,
    JsonElement? Payload,
    string? ChangedBy,
    DateTimeOffset At)
{
    public IEnumerable<string> ChangedValues => Changed is { ValueKind: JsonValueKind.Object } changed ? changed.EnumerateObject().Select(p => p.Name) : [];
}
