using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Blocks.ListBlocks;

internal sealed class ListBlocksHandler(IBlockRepository blocks, IOptions<HomeMixOptions> options)
    : IQueryHandler<ListBlocksQuery, IReadOnlyList<BlockView>>
{
    public async Task<Result<IReadOnlyList<BlockView>>> Handle(ListBlocksQuery query, CancellationToken cancellationToken)
    {
        var configured = options.Value.Blocked.Select(BlockView.FromConfig);
        var owners = (await blocks.ListAsync(cancellationToken)).Select(BlockView.From);
        return Result.Ok<IReadOnlyList<BlockView>>([.. configured, .. owners]);
    }
}
