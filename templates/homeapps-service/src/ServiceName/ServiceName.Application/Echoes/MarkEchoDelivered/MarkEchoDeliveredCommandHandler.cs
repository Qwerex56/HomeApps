using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using ServiceName.Application.Abstractions;
using ServiceName.Domain.Echoes;

namespace ServiceName.Application.Echoes.MarkEchoDelivered;

internal sealed class MarkEchoDeliveredCommandHandler(IEchoRepository echoes, IDateTimeProvider clock)
    : ICommandHandler<MarkEchoDeliveredCommand>
{
    public async Task<Result> Handle(MarkEchoDeliveredCommand request, CancellationToken cancellationToken)
    {
        var echo = await echoes.GetByIdAsync(request.EchoId, cancellationToken);
        if (echo is null)
        {
            return Result.Fail(EchoErrors.NotFound(request.EchoId));
        }

        return echo.MarkDelivered(clock.UtcNow);
    }
}
