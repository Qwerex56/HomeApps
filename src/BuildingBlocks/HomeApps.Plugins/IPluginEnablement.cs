namespace HomeApps.Plugins;

/// <summary>Whether the plugin is switched on in a household. No record means off (jarvis-hub.md 9b.1).</summary>
public interface IPluginEnablement
{
    Task<bool> IsEnabledAsync(Guid householdId, CancellationToken cancellationToken);
}
