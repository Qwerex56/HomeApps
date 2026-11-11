namespace Auth.Api.ConnectedApps;

/// <summary>
/// Browser-based MCP clients (MCP Inspector, claude.ai in the browser) call discovery, registration and the token
/// endpoint from another origin. No cookies are involved, so any origin may.
/// </summary>
internal static class OAuthCors
{
    public const string Policy = "oauth-public";

    public static IServiceCollection AddOAuthCors(this IServiceCollection services) =>
        services.AddCors(cors => cors.AddPolicy(Policy, policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .WithMethods(HttpMethods.Get, HttpMethods.Post)));
}
