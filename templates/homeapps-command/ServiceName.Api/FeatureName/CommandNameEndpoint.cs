using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;
using ServiceName.Application.FeatureName.CommandName;

namespace ServiceName.Api.FeatureName;

internal sealed class CommandNameEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
#if (method == "Delete")
        // DELETE has no body: the command binds from the route and query string.
        app.MapDelete("/featurelower", async ([AsParameters] CommandNameCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttpResult())
#else
        app.MapHttpVerb("/featurelower", async (CommandNameCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttpResult())
#endif
            .WithTags("FeatureName")
            .WithName("CommandName");
    }
}
