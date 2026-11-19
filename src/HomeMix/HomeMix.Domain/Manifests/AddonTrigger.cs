namespace HomeMix.Domain.Manifests;

/// <summary>
/// A server-side reaction (<c>homemix.json</c> <c>triggers[]</c>, household-jarvis-devices.md 2.6): when a household device that
/// fits <paramref name="Match"/> changes <paramref name="Field"/> (<see cref="TriggerKind.State"/>; any value when null) or
/// publishes the event <paramref name="Name"/> (<see cref="TriggerKind.Event"/>; any event when null), the package's
/// <paramref name="Script"/> runs in Jint with the same <c>ctx</c> as a script tool plus <c>ctx.trigger</c> and <c>ctx.devices</c>.
/// </summary>
/// <param name="Id">Names the trigger in the household's list and its saved state; defaults to the script's file name.</param>
public sealed record AddonTrigger(string Id, TriggerKind On, DeviceMatch Match, string? Field, string? Name, string Script)
{
    public const int MaxPerAddon = 10;
    public const string ScriptFolder = "triggers/";
}
