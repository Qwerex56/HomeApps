using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Runs.Ask;

/// <summary>One request without a conversation: the whole answer at once.</summary>
internal sealed class AskHandler(Agent agent) : ICommandHandler<AskCommand, AnswerView>
{
    public async Task<Result<AnswerView>> Handle(AskCommand command, CancellationToken cancellationToken) =>
        await agent.RunAsync(command, streamId: null, cancellationToken);
}
