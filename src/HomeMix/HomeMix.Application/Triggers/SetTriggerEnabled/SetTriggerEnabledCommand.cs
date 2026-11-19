using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Triggers.SetTriggerEnabled;

/// <summary>An admin turns a household add-on's trigger off, or back on (which also clears its failures).</summary>
public sealed record SetTriggerEnabledCommand(Guid InstallationId, string TriggerId, bool Enabled) : ICommand<TriggerView>;
