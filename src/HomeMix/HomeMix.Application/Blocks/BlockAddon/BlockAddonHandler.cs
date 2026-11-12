using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Blocks;

namespace HomeMix.Application.Blocks.BlockAddon;

internal sealed class BlockAddonHandler(IBlockRepository blocks, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<BlockAddonCommand, BlockView>
{
    public async Task<Result<BlockView>> Handle(BlockAddonCommand command, CancellationToken cancellationToken)
    {
        var value = AddonBlock.IsSha256(command.Value) ? command.Value.ToLowerInvariant() : command.Value;
        var block = await blocks.FindAsync(value, cancellationToken);
        if (block is null)
        {
            block = AddonBlock.Create(value, command.Reason, currentUser.RequiredUserId, time.GetUtcNow());
            blocks.Add(block);
        }
        else
        {
            block.Explain(command.Reason);
        }

        return BlockView.From(block);
    }
}
