using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;
using ServiceName.Application.FeatureName.QueryName;

namespace ServiceName.Api.FeatureName;

internal sealed class QueryNameEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/featurelower", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new QueryNameQuery(), ct)).ToHttpResult())
            .WithTags("FeatureName")
            .WithName("QueryName");
    }
}
