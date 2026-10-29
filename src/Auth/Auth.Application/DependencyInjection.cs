using Auth.Application.Abstractions;
using HomeApps.BuildingBlocks.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Application;

public static class DependencyInjection
{
    /// <summary>Handlers, validators and the pipeline from the kit, plus <see cref="AuthOptions"/>. Ports come from Infrastructure.</summary>
    public static IServiceCollection AddAuthApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHomeAppsApplication(typeof(DependencyInjection).Assembly);
        services.AddOptions<AuthOptions>().Bind(configuration.GetSection(AuthOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
