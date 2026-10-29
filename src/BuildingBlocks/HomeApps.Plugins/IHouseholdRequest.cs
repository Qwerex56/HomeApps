namespace HomeApps.Plugins;

/// <summary>
/// A command or query about one household. <see cref="HouseholdPermissionBehavior{TRequest,TResponse}"/> checks it
/// before the handler runs, for HTTP and MCP alike: the caller must be a member, the plugin switched on there, and
/// the caller's role at least the <see cref="Permission"/>'s minimum from the manifest.
/// </summary>
public interface IHouseholdRequest
{
    Guid HouseholdId { get; }

    /// <summary>An id from <c>PluginManifest.Permissions</c>, e.g. <c>tasks.write</c>.</summary>
    string Permission { get; }
}
