using System.Text.Json.Serialization;
using Devices.Api.Link;
using Devices.Api.Mcp;
using Devices.Application.Abstractions;
using HomeApps.BuildingBlocks.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace Devices.Api;

internal static class DependencyInjection
{
    /// <summary>
    /// What only the Devices Api needs: the links (connected devices, in memory), the MCP server whose tools are the
    /// online devices' actions (listed per caller, not from attributes), app tokens of JARVIS's resource, enums as strings.
    /// </summary>
    public static WebApplicationBuilder AddDevicesApi(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddOptions<DevicesOptions>().BindConfiguration(DevicesOptions.SectionName);
        services.AddSingleton<DeviceLinks>();
        services.AddSingleton<IDeviceLinks>(provider => provider.GetRequiredService<DeviceLinks>());
        services.AddScoped<DeviceToolsServer>();
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
