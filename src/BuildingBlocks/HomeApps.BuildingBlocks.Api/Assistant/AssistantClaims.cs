using System.Security.Claims;
using HomeApps.BuildingBlocks.Api.Auth;

namespace HomeApps.BuildingBlocks.Api.Assistant;

public static class AssistantClaims
{
    /// <summary>True when the token was issued to an assistant acting for the person (it carries <c>act</c>).</summary>
    public static bool IsActingAssistant(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.Identity?.IsAuthenticated == true && principal.HasClaim(c => c.Type == ClaimNames.Actor);
    }
}
