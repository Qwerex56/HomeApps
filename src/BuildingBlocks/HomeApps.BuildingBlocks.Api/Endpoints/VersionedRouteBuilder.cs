using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace HomeApps.BuildingBlocks.Api.Endpoints;

/// <summary>The route builder of one pass of <see cref="EndpointExtensions.MapVersionedEndpoints"/>: versioned or legacy.</summary>
internal sealed class VersionedRouteBuilder(IEndpointRouteBuilder inner, bool legacy) : IEndpointRouteBuilder
{
    public bool Legacy { get; } = legacy;

    public IServiceProvider ServiceProvider => inner.ServiceProvider;

    public ICollection<EndpointDataSource> DataSources => inner.DataSources;

    public IApplicationBuilder CreateApplicationBuilder() => inner.CreateApplicationBuilder();
}
