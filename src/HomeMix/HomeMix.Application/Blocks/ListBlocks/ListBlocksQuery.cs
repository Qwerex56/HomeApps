using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Blocks.ListBlocks;

/// <summary>Every block, the configuration's first; for the Owner.</summary>
public sealed record ListBlocksQuery : IQuery<IReadOnlyList<BlockView>>;
