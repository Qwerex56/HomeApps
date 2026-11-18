using System.Security.Cryptography;
using System.Text;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using Households.Application.Assistant;
using Households.Application.Assistant.GetHouseholdAssistant;
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
            Authorized(request, configuration)
                ? (await sender.Send(new ListHouseholdPeopleQuery(id), ct)).ToHttpResult()
                : Results.Unauthorized())
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<IReadOnlyList<MemberView>>();

        // H-2: JARVIS filters a satellite's tools, and caps a person's, by the household's assistant settings.
        app.MapGet("/internal/households/{id:guid}/assistant", async (Guid id, HttpRequest request, IConfiguration configuration, ISender sender, CancellationToken ct) =>
            Authorized(request, configuration)
                ? (await sender.Send(new GetHouseholdAssistantQuery(id), ct)).ToHttpResult()
                : Results.Unauthorized())
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<AssistantView>();
    }

    private static bool Authorized(HttpRequest request, IConfiguration configuration)
    {
        var clientId = configuration["Households:Internal:ClientId"];
        var secret = configuration["Households:Internal:ClientSecret"];
        return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(secret)
            && Same(clientId, request.Headers["X-Client-Id"].ToString()) && Same(secret, request.Headers["X-Client-Secret"].ToString());
    }

    private static bool Same(string expected, string given) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(given)));
}
