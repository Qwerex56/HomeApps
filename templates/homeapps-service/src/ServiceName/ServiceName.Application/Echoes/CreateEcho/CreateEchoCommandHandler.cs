using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using ServiceName.Application.Abstractions;
using ServiceName.Domain.Echoes;

namespace ServiceName.Application.Echoes.CreateEcho;

internal sealed class CreateEchoCommandHandler(
    IEchoRepository echoes,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<CreateEchoCommand, Guid>
{
    public Task<Result<Guid>> Handle(CreateEchoCommand request, CancellationToken cancellationToken)
    {
        var result = Echo.Create(currentUser.RequiredUserId, request.Message, clock.UtcNow);
        if (result.IsFailed)
        {
            return Task.FromResult(result.ToResult<Guid>());
        }

        echoes.Add(result.Value);
        return Task.FromResult(Result.Ok(result.Value.Id));
    }
}
