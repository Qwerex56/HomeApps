using HomeApps.BuildingBlocks.Infrastructure;
using Jarvis.Infrastructure.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Brain;
using Jarvis.Infrastructure.Gateway;
using Jarvis.Infrastructure.Jobs;
using Jarvis.Infrastructure.Persistence;
using Jarvis.Infrastructure.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Postgres with the outbox and the consumers (<c>AssistantInvoked</c>, <c>AssistantConfirmed</c>, <c>UserErased</c>),
    /// the gateway's adapters (Auth's delegation, the sources, MCP clients), the Claude Code brain and the retention job.
    /// </summary>
    public static IServiceCollection AddJarvisInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddHomeAppsInfrastructure<JarvisDbContext>(configuration, "jarvis", typeof(DependencyInjection).Assembly);
        services.AddScoped<IRunRepository, RunRepository>();
        services.AddScoped<IToolCallRepository, ToolCallRepository>();

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
        services.AddScoped<IToolExecutor, GatewayToolExecutor>();

        services.AddOptions<ClaudeCodeOptions>().BindConfiguration(ClaudeCodeOptions.SectionName);
        services.AddScoped<IAssistantBrain, ClaudeCodeBrain>();

        services.AddHostedService<RetentionJob>();
        services.AddSingleton<InvocationQueue>();
        services.AddHostedService<InvocationWorker>();
        return services;
    }
}
