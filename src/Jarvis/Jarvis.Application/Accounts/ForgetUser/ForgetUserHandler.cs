using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;

namespace Jarvis.Application.Accounts.ForgetUser;

internal sealed class ForgetUserHandler(IRunRepository runs, IConversationRepository conversations) : ICommandHandler<ForgetUserCommand>
{
    public async Task<Result> Handle(ForgetUserCommand command, CancellationToken cancellationToken)
    {
        await runs.ForgetAsync(command.UserId, cancellationToken);
        await conversations.ForgetAsync(command.UserId, cancellationToken);
        return Result.Ok();
    }
}
