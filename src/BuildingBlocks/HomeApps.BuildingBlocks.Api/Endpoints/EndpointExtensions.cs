using System.Reflection;
using Microsoft.AspNetCore.Builder;
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

    /// <summary>Maps every registered <see cref="IEndpoint"/>, optionally under a shared route group.</summary>
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
}
