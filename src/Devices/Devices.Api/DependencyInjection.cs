using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Devices.Api.Link;
using Devices.Api.Mcp;
using Devices.Api.Stream;
using Devices.Application.Abstractions;
using HomeApps.BuildingBlocks.Api.Auth;
using HomeApps.BuildingBlocks.Api.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace Devices.Api;

internal static class DependencyInjection
{
    /// <summary>60 reports per minute per device token (its subject).</summary>
    public const string ReportRateLimit = "devices-report";

    /// <summary>
    /// What only the Devices Api needs: the links (connected devices, in memory), the stream (SSE) of a household's changes, the MCP server whose tools are the
    /// online devices' actions (listed per caller, not from attributes), app tokens of JARVIS's resource, enums as strings.
    /// </summary>
    public static WebApplicationBuilder AddDevicesApi(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddOptions<DevicesOptions>().BindConfiguration(DevicesOptions.SectionName);
        services.AddSingleton<DeviceLinks>();
        services.AddSingleton<IDeviceLinks>(provider => provider.GetRequiredService<DeviceLinks>());
        services.AddSingleton<DeviceStream>();
        services.AddSingleton<IDeviceStream>(provider => provider.GetRequiredService<DeviceStream>());
        services.AddScoped<DeviceToolsServer>();
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                http.Response.Headers.RetryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture)
                    : "60";
                await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = http,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "Too many reports in a short time. Wait a moment and try again.",
                        Extensions = { [ResultHttpMapper.CodeExtension] = "rate_limited" },
                    },
                });
            };

            limiter.AddPolicy(ReportRateLimit, http => RateLimitPartition.GetSlidingWindowLimiter(
                http.User.FindFirst(ClaimNames.Subject)?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new SlidingWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6, QueueLimit = 0 }));
        });

        services.AddMcpServer(options => options.ServerInfo = new Implementation
            {
                Name = "homeapps-devices",
                Version = typeof(DependencyInjection).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
            })
            .WithHttpTransport(options => options.Stateless = true)
            .WithListToolsHandler((context, ct) => Tools(context.Services).ListToolsAsync(ct))
            .WithCallToolHandler((context, ct) => Tools(context.Services).CallToolAsync(context.Params!, ct));

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .PostConfigure<IOptions<DevicesOptions>, IOptions<JwtOptions>>((bearer, devices, jwt) =>
                bearer.TokenValidationParameters.ValidAudiences = string.IsNullOrWhiteSpace(devices.Value.Resource)
                    ? [jwt.Value.Audience]
                    : [jwt.Value.Audience, devices.Value.Resource]);

        return builder;
    }

    private static DeviceToolsServer Tools(IServiceProvider? services) =>
        (services ?? throw new InvalidOperationException("An MCP request without services.")).GetRequiredService<DeviceToolsServer>();
}
