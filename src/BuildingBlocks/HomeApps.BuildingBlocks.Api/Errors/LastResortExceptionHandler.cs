using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HomeApps.BuildingBlocks.Api.Errors;

/// <summary>
/// Catches what nothing else handled. Business rules return typed errors instead of throwing, so reaching this
/// handler means a bug or an outage: it logs the exception and answers a generic 500 that never echoes
/// <see cref="Exception.Message"/> to the caller.
/// </summary>
internal sealed partial class LastResortExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<LastResortExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client went away; nothing to answer.
            return true;
        }

        LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Detail = "The server could not complete the request. Try again later.",
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, PathString path);
}
