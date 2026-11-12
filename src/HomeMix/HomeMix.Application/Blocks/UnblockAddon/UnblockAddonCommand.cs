using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Blocks.UnblockAddon;

/// <summary>Lifts one of the Owner's blocks; its installations are served again.</summary>
public sealed record UnblockAddonCommand(string Value) : ICommand;
