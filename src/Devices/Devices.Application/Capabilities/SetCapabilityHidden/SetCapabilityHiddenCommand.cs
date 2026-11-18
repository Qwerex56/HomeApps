using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Capabilities.SetCapabilityHidden;

/// <summary>The Owner hides a registered capability (from add-on matching and suggestions) or shows it again. Promotion to a built-in is a release step.</summary>
public sealed record SetCapabilityHiddenCommand(string Name, bool Hidden) : ICommand;
