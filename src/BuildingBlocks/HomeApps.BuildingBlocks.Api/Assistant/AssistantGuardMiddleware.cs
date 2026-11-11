using FluentResults;
using HomeApps.BuildingBlocks.Api.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HomeApps.BuildingBlocks.Api.Assistant;

/// <summary>
/// Runs after authentication and authorization. A request whose token carries <c>act</c> is refused with 403
/// <c>assistant.not_allowed</c> when the endpoint denies assistants, or when it changes data (not GET, HEAD or
/// OPTIONS) in a service that denies writes by default and the endpoint does not allow assistants.
/// </summary>
internal sealed class AssistantGuardMiddleware(RequestDelegate next, IOptions<AssistantGuardOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.IsActingAssistant() && !IsAllowed(context))
        {
            await Result.Fail(AssistantErrors.NotAllowed).ToHttpResult().ExecuteAsync(context);
            return;
        }

        await next(context);
    }

    private bool IsAllowed(HttpContext context)
    {
        var access = context.GetEndpoint()?.Metadata.GetMetadata<AssistantAccessMetadata>()?.Access ?? AssistantAccess.Default;
        return access switch
        {
            AssistantAccess.Allow => true,
            AssistantAccess.Deny => false,
            _ => !options.Value.DenyWritesByDefault || IsRead(context.Request.Method),
        };
    }

    private static bool IsRead(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);
}
