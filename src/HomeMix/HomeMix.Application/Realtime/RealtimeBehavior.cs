using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HomeMix.Application.Realtime;

/// <summary>
/// Sends what a command put in the <see cref="RealtimeOutbox"/> once it succeeded. Registered before the kit's
/// pipeline, so it wraps the transaction and runs after the commit. A failed send is logged, never the command's failure:
/// realtime is a hint to read again, the data is already saved.
/// </summary>
public sealed partial class RealtimeBehavior<TRequest, TResponse>(
    RealtimeOutbox outbox, IRealtimeSender sender, ILogger<RealtimeBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
    where TResponse : ResultBase
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var response = await next(cancellationToken);
        var messages = outbox.TakeAll();
        if (response.IsSuccess && messages.Count > 0)
        {
            try
            {
                await sender.SendAsync(messages, CancellationToken.None);
            }
#pragma warning disable CA1031 // Realtime must never fail a saved command.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogSendFailed(logger, exception, typeof(TRequest).Name);
            }
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Realtime events after {Command} were not sent")]
    private static partial void LogSendFailed(ILogger logger, Exception exception, string command);
}
