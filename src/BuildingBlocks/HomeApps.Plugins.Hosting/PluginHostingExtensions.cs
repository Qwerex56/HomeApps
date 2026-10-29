using System.Reflection;
using System.Text.Json.Serialization;
using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure;
using HomeApps.BuildingBlocks.Infrastructure.Options;
using HomeApps.Plugins.Manifest;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace HomeApps.Plugins.Hosting;

/// <summary>
/// A plugin's Program.cs in three calls (docs/plugins.md):
/// <code>
/// builder.AddHomeAppsPlugin(TodoPlugin.Manifest, typeof(Program).Assembly);
/// builder.Services.AddHomeAppsPluginInfrastructure&lt;TodoDbContext&gt;(builder.Configuration, TodoPlugin.Manifest, consumersAssembly);
/// app.UseHomeAppsPlugin();
/// </code>
/// </summary>
public static class PluginHostingExtensions
{
    /// <summary>
    /// <see cref="HomeAppsApiExtensions.AddHomeAppsDefaults"/> plus the plugin's manifest, enums as strings like the
    /// core services, and <see cref="IHouseholdAccess"/> over HTTP with its cache (section <c>Plugin</c>).
    /// </summary>
    public static WebApplicationBuilder AddHomeAppsPlugin(this WebApplicationBuilder builder, PluginManifest manifest, Assembly endpointsAssembly)
    {
        ArgumentNullException.ThrowIfNull(builder);
        PluginManifestValidator.EnsureValid(manifest);

        builder.AddHomeAppsDefaults(endpointsAssembly);
        var services = builder.Services;

        services.TryAddSingleton(manifest);
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddValidatedOptions<PluginHostingOptions>(PluginHostingOptions.SectionName);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<HouseholdAccessCache>();
        services.AddHttpClient<IHouseholdAccess, HouseholdsHttpAccess>((sp, http) =>
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<PluginHostingOptions>>().Value.HouseholdsUrl.TrimEnd('/') + "/"));

        return builder;
    }

    /// <summary>
    /// <see cref="DependencyInjection.AddHomeAppsInfrastructure{TContext}(IServiceCollection, IConfiguration, string, Assembly, Action{IRetryConfigurator}?)"/>
    /// for a plugin: the queue prefix is the plugin id, the kit's own consumer (cache of household roles) runs next to
    /// the plugin's, and <see cref="IPluginEnablement"/> reads the plugin's table.
    /// </summary>
    public static IServiceCollection AddHomeAppsPluginInfrastructure<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        PluginManifest manifest,
        Assembly consumersAssembly,
        Action<IRetryConfigurator>? retry = null)
        where TContext : PluginDbContext
    {
        ArgumentNullException.ThrowIfNull(manifest);

        services.AddHomeAppsInfrastructure<TContext>(
            configuration, manifest.Id, [consumersAssembly, typeof(PluginHostingExtensions).Assembly], retry);
        services.AddScoped<PluginDbContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<IPluginEnablement, PluginEnablementStore>();

        return services;
    }

    /// <summary>
    /// Everything under <c>/plugins/{id}</c>, the prefix Traefik routes to the plugin (it does not strip it):
    /// the plugin's endpoints under <c>/api</c>, the manifest at <c>/manifest</c>, OpenAPI at <c>/openapi/v1.json</c>
    /// (always, the web app and JARVIS read it), health at <c>/health/*</c> and the built UI from <c>wwwroot/ui</c>
    /// at <c>/ui</c>, anonymous and never cached, since <c>entry.js</c> keeps its name between versions.
    /// </summary>
    public static WebApplication UseHomeAppsPlugin(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var manifest = app.Services.GetRequiredService<PluginManifest>();

        app.UsePathBase(manifest.BasePath);
        app.UseRouting();

        var ui = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "ui");
        if (Directory.Exists(ui))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = "/ui",
                FileProvider = new PhysicalFileProvider(ui),
                OnPrepareResponse = context => context.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache",
            });
        }

        app.UseHomeAppsDefaults(endpointsPrefix: "/api");
        if (!app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        app.MapPluginEndpoints(manifest);

        return app;
    }
}
