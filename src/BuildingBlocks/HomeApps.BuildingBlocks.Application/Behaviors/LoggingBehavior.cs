using System.Diagnostics;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HomeApps.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Logs each request's name, duration and outcome. It never logs the request body, so secrets in commands stay out of logs.
/// </summary>
public sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : ResultBase
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var started = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken).ConfigureAwait(false);

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (response.IsSuccess)
        {
            LogSucceeded(logger, requestName, elapsedMs);
        }
        else
        {
            var errors = string.Join("; ", response.Errors.Select(e => e.GetType().Name + ": " + e.Message));
            LogFailed(logger, requestName, elapsedMs, errors);
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{RequestName} succeeded in {ElapsedMs:0.0} ms")]
    private static partial void LogSucceeded(ILogger logger, string requestName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestName} failed in {ElapsedMs:0.0} ms: {Errors}")]
    private static partial void LogFailed(ILogger logger, string requestName, double elapsedMs, string errors);
}
