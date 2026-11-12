using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Blocks;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Blocks.UnblockAddon;

internal sealed class UnblockAddonHandler(IBlockRepository blocks, IOptions<HomeMixOptions> options) : ICommandHandler<UnblockAddonCommand>
{
    public async Task<Result> Handle(UnblockAddonCommand command, CancellationToken cancellationToken)
    {
        var value = AddonBlock.IsSha256(command.Value) ? command.Value.ToLowerInvariant() : command.Value;
        var block = await blocks.FindAsync(value, cancellationToken);
        if (block is null)
        {
            return options.Value.Blocked.Contains(command.Value, StringComparer.OrdinalIgnoreCase)
                ? Result.Fail(BlockErrors.InConfiguration)
                : Result.Fail(BlockErrors.NotFound);
        }

        blocks.Remove(block);
        return Result.Ok();
    }
}
