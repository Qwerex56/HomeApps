using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using ServiceName.Application.Abstractions;
using ServiceName.Domain.Echoes;

namespace ServiceName.Application.Echoes.GetEcho;

internal sealed class GetEchoQueryHandler(IEchoRepository echoes, ICurrentUser currentUser)
    : IQueryHandler<GetEchoQuery, EchoResponse>
{
    public async Task<Result<EchoResponse>> Handle(GetEchoQuery request, CancellationToken cancellationToken)
    {
        var echo = await echoes.GetByIdAsync(request.EchoId, cancellationToken);
        if (echo is null)
        {
            return Result.Fail(EchoErrors.NotFound(request.EchoId));
        }

        var owned = echo.EnsureOwnedBy(currentUser.RequiredUserId);
        if (owned.IsFailed)
        {
            return owned;
        }

        return new EchoResponse(echo.Id, echo.Message, echo.CreatedAt, echo.DeliveredAt);
    }
}
