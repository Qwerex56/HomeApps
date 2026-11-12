using HomeApps.BuildingBlocks.Api.Auth;
using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Gateway;
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

    public Guid UserId => Guid.Parse(User.FindFirst(ClaimNames.Subject)!.Value);

    /// <summary>JARVIS's own run (<see cref="RunHeader"/>): only with a delegated token, which only JARVIS and Auth make.</summary>
    public Guid? RunId =>
        User.HasClaim(c => c.Type == ClaimNames.Actor)
        && Guid.TryParse(Context.Request.Headers[RunHeader.Name].ToString(), out var run) ? run : null;

    private HttpContext Context =>
        accessor.HttpContext ?? throw new InvalidOperationException("The gateway runs only inside an HTTP request.");

    private System.Security.Claims.ClaimsPrincipal User => Context.User;

    public async Task<string> SourceTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is not null)
        {
            return _token;
        }

        if (User.HasClaim(c => c.Type == ClaimNames.Actor))
        {
            _token = Context.Request.Headers[HeaderNames.Authorization].ToString()["Bearer ".Length..].Trim();
            return _token;
        }

        var client = User.FindFirst(ClaimNames.ClientId)?.Value ?? DirectClient;
        _token = await delegation.TokenForAsync(UserId, "mcp", client, cancellationToken);
        return _token;
    }
}
