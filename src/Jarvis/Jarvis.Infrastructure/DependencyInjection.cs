using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure;

public static class DependencyInjection
{
    /// <summary>The gateway's adapters: Auth's delegation, the sources and the MCP clients to them.</summary>
    public static IServiceCollection AddJarvisInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<GatewayOptions>()
            .BindConfiguration(GatewayOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddMemoryCache();
        services.AddHttpClient<IDelegation, AuthDelegation>((sp, http) =>
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<GatewayOptions>>().Value.AuthUrl.TrimEnd('/') + "/"));
        services.AddHttpClient<IToolSources, ConfiguredToolSources>();
        services.AddHttpClient(SourceConnector.HttpClientName);
        services.AddSingleton<SourceConnector>();
        return services;
    }
}
