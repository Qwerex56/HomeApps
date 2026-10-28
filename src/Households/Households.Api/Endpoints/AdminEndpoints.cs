using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using Households.Application.Admin;
using Households.Application.Common;
using MediatR;

namespace Households.Api.Endpoints;

/// <summary>
/// Q7: every household for a server Admin or Owner (D8), newest first, 50 a page; <c>before</c> is the last
/// <c>createdAt</c> of the previous page. Archiving and restoring go through <c>/households/{id}</c> like for members.
/// </summary>
internal sealed class AdminEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/households").WithTags("Administration").RequireAuthorization(Policies.Admin);

        admin.MapGet("/", async (string? q, DateTimeOffset? before, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListAllHouseholdsQuery(q, before), ct)).ToHttpResult())
            .Produces<IReadOnlyList<AdminHouseholdView>>();
    }
}
