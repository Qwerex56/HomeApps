using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Devices.Api.Tests;

/// <summary>
/// The test server has no remote address; this gives every request the loopback one, a known proxy by default, so the
/// forwarded-headers middleware reads <c>X-Forwarded-For</c> like it does behind Traefik.
/// </summary>
internal sealed class FixedRemoteIpFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            return nextMiddleware(context);
        });
        next(app);
    };
}
