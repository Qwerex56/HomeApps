using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace HomeApps.BuildingBlocks.Api.Endpoints;

/// <summary>
/// The route builder of one pass of <see cref="EndpointExtensions.MapVersionedEndpoints"/>: versioned or legacy.
/// Routes mapped on it directly (the ones that stay unversioned, such as OAuth or the sockets) land in the application only
/// on the versioned pass; on the legacy pass they go nowhere, so they are not registered twice.
/// </summary>
internal sealed class VersionedRouteBuilder(IEndpointRouteBuilder inner, bool legacy) : IEndpointRouteBuilder
{
    private readonly List<EndpointDataSource> _discarded = [];

    public bool Legacy { get; } = legacy;

    /// <summary>The application's own builder: where the legacy group of <see cref="EndpointExtensions.MapVersionedGroup"/> goes.</summary>
    public IEndpointRouteBuilder Inner { get; } = inner;

    public IServiceProvider ServiceProvider => Inner.ServiceProvider;

    public ICollection<EndpointDataSource> DataSources => Legacy ? _discarded : Inner.DataSources;

    public IApplicationBuilder CreateApplicationBuilder() => Inner.CreateApplicationBuilder();
}
