using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Blocks.BlockAddon;

/// <summary>The Owner blocks an add-on id or one package SHA-256; blocking it again only changes the reason.</summary>
public sealed record BlockAddonCommand(string Value, string? Reason) : ICommand<BlockView>;
