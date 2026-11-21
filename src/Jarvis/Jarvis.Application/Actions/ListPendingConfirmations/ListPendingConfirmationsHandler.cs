using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.Actions.ListPendingConfirmations;

internal sealed class ListPendingConfirmationsHandler(IToolCallRepository calls, TimeProvider time)
    : IQueryHandler<ListPendingConfirmationsQuery, IReadOnlyList<ActionView>>
{
    public async Task<Result<IReadOnlyList<ActionView>>> Handle(ListPendingConfirmationsQuery query, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var pending = await calls.ListPendingForUserAsync(query.UserId, cancellationToken);
        var filtered = pending
            .Where(c => c.StatusAt(now) == ToolCallStatus.NeedsConfirmation)
            .OrderByDescending(c => c.CalledAt)
            .Select(c => ActionView.From(c, now))
            .ToList();

        return Result.Ok<IReadOnlyList<ActionView>>(filtered);
    }
}
