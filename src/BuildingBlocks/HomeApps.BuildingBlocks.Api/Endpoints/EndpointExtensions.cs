using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeApps.BuildingBlocks.Api.Endpoints;

public static class EndpointExtensions
{
    /// <summary>Registers every non-abstract <see cref="IEndpoint"/> in the assembly.</summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var descriptors = assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IEndpoint)))
            .Select(t => ServiceDescriptor.Transient(typeof(IEndpoint), t));

        services.TryAddEnumerable(descriptors);
        return services;
    }

    /// <summary>
    /// Maps every registered <see cref="IEndpoint"/> once, optionally under a shared route group.
    /// </summary>
    public static IApplicationBuilder MapEndpoints(this WebApplication app, RouteGroupBuilder? group = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        IEndpointRouteBuilder builder = group is null ? app : group;
        foreach (var endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }

    /// <summary>
    /// Maps every registered <see cref="IEndpoint"/> twice: under <c>/v1</c> (see <see cref="MapVersionedGroup"/>) and as the
    /// legacy routes without it, which answer with <c>Deprecation</c> and <c>Sunset</c> headers and stay out of the OpenAPI document.
    /// </summary>
    public static IApplicationBuilder MapVersionedEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>().ToList();
        foreach (var legacy in new[] { false, true })
        {
            var builder = new VersionedRouteBuilder(app, legacy);
            foreach (var endpoint in endpoints)
            {
                endpoint.MapEndpoint(builder);
            }
        }

        return app;
    }

    /// <summary>
    /// A group for <c>{prefix}/v1</c>; when the endpoints are mapped by <see cref="MapVersionedEndpoints"/> its second pass
    /// returns the legacy <c>{prefix}</c> group instead, so one <see cref="IEndpoint"/> class serves both route sets.
    /// </summary>
    public static RouteGroupBuilder MapVersionedGroup(this IEndpointRouteBuilder app, string prefix)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app is not VersionedRouteBuilder { Legacy: true })
        {
            return app.MapGroup($"{prefix}/v1");
        }

        return app.MapLegacyGroup(prefix);
    }

    /// <summary>Marks a route group as the legacy (unversioned) set of the 3.x line: deprecated, removed in 4.0.</summary>
    public static RouteGroupBuilder MapLegacyGroup(this IEndpointRouteBuilder app, string prefix)
    {
        ArgumentNullException.ThrowIfNull(app);

        var legacy = app.MapGroup(prefix);
        legacy.ExcludeFromDescription();
        legacy.AddEndpointFilter(async (context, next) =>
        {
            AddDeprecationHeaders(context.HttpContext.Response);
            return await next(context);
        });
        return legacy;
    }

    private static void AddDeprecationHeaders(HttpResponse response)
    {
        response.Headers["Deprecation"] = "true";
        response.Headers["Sunset"] = "Sat, 01 Jan 2028 00:00:00 GMT";
    }
}
