using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Realtime;
using HomeMix.Domain.Blocks;

namespace HomeMix.Application.Blocks.BlockAddon;

internal sealed class BlockAddonHandler(
    IBlockRepository blocks, IInstallationRepository installations, ICurrentUser currentUser, TimeProvider time, RealtimeOutbox realtime)
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
            await BlockedInstallations.AnnounceAsync(value, "blocked", installations, realtime, cancellationToken);
        }
        else
        {
            block.Explain(command.Reason);
        }

        return BlockView.From(block);
    }
}
