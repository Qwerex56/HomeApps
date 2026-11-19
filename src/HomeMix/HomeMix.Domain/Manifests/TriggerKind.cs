namespace HomeMix.Domain.Manifests;

/// <summary>What a trigger reacts to: a change of a device's state values or an event the device published (household-jarvis-devices.md 2.6).</summary>
public enum TriggerKind
{
    State,
    Event,
}
