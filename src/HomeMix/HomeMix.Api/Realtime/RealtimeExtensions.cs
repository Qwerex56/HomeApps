using HomeMix.Application.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;

namespace HomeMix.Api.Realtime;

internal static class RealtimeExtensions
{
    private const string AccessTokenQuery = "access_token";

    /// <summary>
    /// SignalR with the hub's sender. Browsers cannot put a header on a WebSocket, so on the hub's path only the token
    /// comes from <c>?access_token=</c> and is copied to the Authorization header, which Households is asked with.
    /// </summary>
    public static IServiceCollection AddHomeMixRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
        services.AddSingleton<IRealtimeSender, HubRealtimeSender>();
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, bearer =>
        {
            var previous = bearer.Events?.OnMessageReceived;
            bearer.Events ??= new JwtBearerEvents();
            bearer.Events.OnMessageReceived = async context =>
            {
                if (previous is not null)
                {
                    await previous(context);
                }

                if (context.Token is null && context.Request.Path.StartsWithSegments(HomeMixHub.Path)
                    && context.Request.Query[AccessTokenQuery].ToString() is { Length: > 0 } token)
                {
                    context.Token = token;
                    context.Request.Headers.Authorization = $"Bearer {token}";
                }
            };
        });
        return services;
    }

    public static IEndpointRouteBuilder MapHomeMixRealtime(this IEndpointRouteBuilder app)
    {
        app.MapHub<HomeMixHub>(HomeMixHub.Path);
        return app;
    }
}
