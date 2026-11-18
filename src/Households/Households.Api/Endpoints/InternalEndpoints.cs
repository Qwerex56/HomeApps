using System.Security.Cryptography;
using System.Text;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using Households.Application.Common;
using Households.Application.Members.ListHouseholdPeople;
using MediatR;

namespace Households.Api.Endpoints;

/// <summary>
/// Service to service, inside the Docker network (like Auth's <c>/internal/*</c>): the JARVIS service client proves
/// itself with its secret (<c>Households:Internal:ClientId</c>/<c>ClientSecret</c>, sent as <c>X-Client-Id</c>/<c>X-Client-Secret</c>,
/// compared in constant time). Not reachable through the gateway; no secret configured means always 401.
/// </summary>
internal sealed class InternalEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // H-0: JARVIS reads the people for the spoken PIN (a satellite is not a member, so it cannot use /members).
        app.MapGet("/internal/households/{id:guid}/members", async (Guid id, HttpRequest request, IConfiguration configuration, ISender sender, CancellationToken ct) =>
        {
            var clientId = configuration["Households:Internal:ClientId"];
            var secret = configuration["Households:Internal:ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secret)
                || !Same(clientId, request.Headers["X-Client-Id"].ToString()) || !Same(secret, request.Headers["X-Client-Secret"].ToString()))
            {
                return Results.Unauthorized();
            }

            return (await sender.Send(new ListHouseholdPeopleQuery(id), ct)).ToHttpResult();
        })
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<IReadOnlyList<MemberView>>();
    }

    private static bool Same(string expected, string given) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(given)));
}
