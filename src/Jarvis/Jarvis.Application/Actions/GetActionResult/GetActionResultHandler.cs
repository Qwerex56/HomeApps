using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.Actions.GetActionResult;

internal sealed class GetActionResultHandler(IToolCallRepository calls, TimeProvider time)
    : IQueryHandler<GetActionResultQuery, ActionResultView>
{
    public async Task<Result<ActionResultView>> Handle(GetActionResultQuery query, CancellationToken cancellationToken)
    {
        var call = await calls.FindAsync(query.ActionId, cancellationToken);
        if (call is null || call.UserId != query.UserId)
        {
            return Result.Fail(ToolCallErrors.NotFound);
        }

        var statusAt = call.StatusAt(time.GetUtcNow());
        return statusAt switch
        {
            ToolCallStatus.NeedsConfirmation => new ActionResultView("pending", null),
            ToolCallStatus.Done => new ActionResultView("approved", call.Result),
            ToolCallStatus.Denied => new ActionResultView("denied", null),
            ToolCallStatus.Expired => new ActionResultView("expired", null),
            ToolCallStatus.Failed => new ActionResultView("failed", call.Result),
            _ => new ActionResultView(statusAt.ToString().ToLowerInvariant(), call.Result),
        };
    }
}
