using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using MediatR;

namespace HomeApps.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Saves the unit of work after a command succeeds, so handlers only load, call a domain method and return.
/// A failed result saves nothing. Queries pass straight through.
/// Saving also dispatches domain events and writes outbox messages, all in one database transaction.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
    where TResponse : ResultBase
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var response = await next(cancellationToken).ConfigureAwait(false);
        if (response.IsSuccess)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
