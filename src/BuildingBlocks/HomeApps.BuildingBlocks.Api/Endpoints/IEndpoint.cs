using Microsoft.AspNetCore.Routing;

namespace HomeApps.BuildingBlocks.Api.Endpoints;

/// <summary>
/// One endpoint (or a small group) per class. Classes are found by assembly scanning, so adding a use case never
/// means editing Program.cs. Endpoints require authentication unless they call <c>AllowAnonymous()</c>.
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
