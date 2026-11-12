using HomeApps.BuildingBlocks.Application;
using Jarvis.Application.Conversations;
using Jarvis.Application.Runs;
using Jarvis.Application.ToolCalls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jarvis.Application;

public static class DependencyInjection
{
    /// <summary>Handlers, validators and the kit's pipeline; ports come from Infrastructure.</summary>
    public static IServiceCollection AddJarvisApplication(this IServiceCollection services)
    {
        services.AddHomeAppsApplication(typeof(DependencyInjection).Assembly);
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<AssistantOptions>().BindConfiguration(AssistantOptions.SectionName);
        services.AddScoped<RunAccess>();
        services.AddScoped<Agent>();
        services.AddScoped<OwnConversation>();
        return services;
    }
}
