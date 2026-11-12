using HomeApps.BuildingBlocks.Api.Auth;
using HomeApps.BuildingBlocks.Mcp;
using Jarvis.Application.Gateway;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;

namespace Jarvis.Api.Gateway;

internal static class GatewayExtensions
{
    /// <summary>Where MCP clients connect, behind Traefik's <c>/jarvis</c>.</summary>
    public const string Path = "/jarvis/mcp";

    /// <summary>RFC 9728: the resource's path after the well-known prefix.</summary>
    public const string MetadataPath = "/.well-known/oauth-protected-resource" + Path;

    /// <summary>
    /// The gateway's MCP server (stateless Streamable HTTP, handlers instead of tool classes) and its OAuth side:
    /// tokens with <c>aud</c> = <see cref="GatewayOptions.Resource"/> (connected apps) are accepted next to the usual
    /// <c>homeapps</c>, and a request without a token gets 401 with <c>resource_metadata</c>, from which Claude Code
    /// and other clients find Auth and start the OAuth flow (jarvis-v1.md 3.1).
    /// </summary>
    public static WebApplicationBuilder AddJarvisGateway(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddScoped<Caller>();
        services.AddScoped<McpGateway>();

        services.AddMcpServer(options => options.ServerInfo = new Implementation
            {
                Name = "homeapps-jarvis",
                Version = typeof(GatewayExtensions).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
            })
            .WithHttpTransport(options => options.Stateless = true)
            .WithListToolsHandler((context, ct) => Gateway(context.Services).ListToolsAsync(ct))
            .WithCallToolHandler((context, ct) => Gateway(context.Services).CallToolAsync(context.Params!, ct))
            .WithListResourcesHandler((context, ct) => Gateway(context.Services).ListResourcesAsync(ct))
            .WithReadResourceHandler((context, ct) => Gateway(context.Services).ReadResourceAsync(context.Params!, ct));

        services.AddAuthentication().AddMcp();
        services.Configure<AuthenticationOptions>(options =>
            options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme);
        services.AddOptions<McpAuthenticationOptions>(McpAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<GatewayOptions>, IOptions<JwtOptions>>((mcp, gateway, jwt) =>
            {
                mcp.ResourceMetadataUri = new Uri(MetadataPath, UriKind.Relative);
                mcp.ResourceMetadata = new ProtectedResourceMetadata
                {
                    Resource = gateway.Value.Resource,
                    AuthorizationServers = [string.IsNullOrWhiteSpace(jwt.Value.Issuer) ? jwt.Value.Authority : jwt.Value.Issuer],
                    ScopesSupported = ["homeapps"],
                    BearerMethodsSupported = ["header"],
                    ResourceName = "HomeApps (JARVIS)",
                };
            });
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .PostConfigure<IOptions<GatewayOptions>, IOptions<JwtOptions>>((bearer, gateway, jwt) =>
                bearer.TokenValidationParameters.ValidAudiences = [jwt.Value.Audience, gateway.Value.Resource]);

        return builder;
    }

    public static WebApplication MapJarvisGateway(this WebApplication app)
    {
        app.MapHomeAppsMcp(Path);
        return app;
    }

    private static McpGateway Gateway(IServiceProvider? services) =>
        (services ?? throw new InvalidOperationException("An MCP request without services.")).GetRequiredService<McpGateway>();
}
