using System.Reflection;
using HomeApps.BuildingBlocks.Application;
using HomeApps.Plugins.Manifest;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeApps.Plugins;

public static class DependencyInjection
{
    /// <summary>
    /// <see cref="HomeApps.BuildingBlocks.Application.DependencyInjection.AddHomeAppsApplication"/> plus the household
    /// permission check, which runs last, right before the handler. The manifest is validated here, so a broken one
    /// stops the plugin at start.
    /// </summary>
    public static IServiceCollection AddHomeAppsPluginApplication(
        this IServiceCollection services, PluginManifest manifest, Assembly applicationAssembly)
    {
        PluginManifestValidator.EnsureValid(manifest);

        services.TryAddSingleton(manifest);
        services.AddHomeAppsApplication(applicationAssembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HouseholdPermissionBehavior<,>));

        return services;
    }
}
