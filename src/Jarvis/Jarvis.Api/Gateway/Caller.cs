using HomeApps.BuildingBlocks.Api.Auth;
using Jarvis.Application.Gateway;
using Microsoft.Net.Http.Headers;

namespace Jarvis.Api.Gateway;

/// <summary>
/// Who called the gateway, and the token the sources get for them: a delegated token (<c>act</c>) passes as it is
/// (JARVIS's own loop); any other token, a connected app's or a person's own, is exchanged for one, so every source
/// sees an assistant acting for the person (JV3, JV13).
/// </summary>
internal sealed class Caller(IHttpContextAccessor accessor, IDelegation delegation)
{
    public const string DirectClient = "direct";

    private string? _token;

    public async Task<string> SourceTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is not null)
        {
            return _token;
        }

        var context = accessor.HttpContext ?? throw new InvalidOperationException("The gateway runs only inside an HTTP request.");
        var user = context.User;
        if (user.HasClaim(c => c.Type == ClaimNames.Actor))
        {
            var header = context.Request.Headers[HeaderNames.Authorization].ToString();
            _token = header["Bearer ".Length..].Trim();
            return _token;
        }

        var userId = Guid.Parse(user.FindFirst(ClaimNames.Subject)!.Value);
        var client = user.FindFirst(ClaimNames.ClientId)?.Value ?? DirectClient;
        _token = await delegation.TokenForAsync(userId, client, cancellationToken);
        return _token;
    }
}
