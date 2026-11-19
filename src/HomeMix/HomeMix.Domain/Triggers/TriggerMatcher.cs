using HomeMix.Domain.Devices;
using HomeMix.Domain.Manifests;

namespace HomeMix.Domain.Triggers;

/// <summary>
/// Does a device's change or event fire a trigger? The device must fit the trigger's capability, role and model like a widget's
/// (<see cref="DeviceMatcher.Fits"/>); a state trigger also needs its field among the changed values (a value of an instance is
/// <c>lampka.on</c>, which the field <c>on</c> matches too), an event trigger its name when it has one.
/// </summary>
public static class TriggerMatcher
{
    public static bool MatchesState(AddonTrigger trigger, DeviceCandidate device, IEnumerable<string> changedValues)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(changedValues);
        return trigger.On == TriggerKind.State
            && DeviceMatcher.Fits(trigger.Match, device)
            && (trigger.Field is null || changedValues.Any(v => v == trigger.Field || v.EndsWith("." + trigger.Field, StringComparison.Ordinal)));
    }

    public static bool MatchesEvent(AddonTrigger trigger, DeviceCandidate device, string eventName)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        return trigger.On == TriggerKind.Event
            && DeviceMatcher.Fits(trigger.Match, device)
            && (trigger.Name is null || trigger.Name == eventName);
    }
}
