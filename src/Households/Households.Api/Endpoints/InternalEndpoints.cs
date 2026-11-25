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
/// itself with its secret (<c>Households:Internal:ClientId</c>/<c>ClientSecret</c>, or for Devices <c>Households:Internal:Devices:*</c>; sent as <c>X-Client-Id</c>/<c>X-Client-Secret</c>,
/// compared in constant time). Not reachable through the gateway; no secret configured means always 401.
/// </summary>
internal sealed class InternalEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var internalApi = app.MapVersionedGroup("/internal");

        // H-0: JARVIS reads the people for the spoken PIN (a satellite is not a member, so it cannot use /members).
        internalApi.MapGet("/households/{id:guid}/members", async (Guid id, HttpRequest request, IConfiguration configuration, ISender sender, CancellationToken ct) =>
            Authorized(request, configuration)
                ? (await sender.Send(new ListHouseholdPeopleQuery(id), ct)).ToHttpResult()
                : Results.Unauthorized())
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<IReadOnlyList<MemberView>>();

        // H-2: JARVIS filters a satellite's tools, and caps a person's, by the household's assistant settings.
        internalApi.MapGet("/households/{id:guid}/assistant", async (Guid id, HttpRequest request, IConfiguration configuration, ISender sender, CancellationToken ct) =>
            Authorized(request, configuration)
                ? (await sender.Send(new GetHouseholdAssistantQuery(id), ct)).ToHttpResult()
                : Results.Unauthorized())
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<AssistantView>();
    }

    /// <summary>The JARVIS client (<c>Households:Internal</c>), the Devices client (<c>Households:Internal:Devices</c>) or HomeMix's (<c>Households:Internal:HomeMix</c>, who the admins are for its triggers), each with its own secret.</summary>
    private static bool Authorized(HttpRequest request, IConfiguration configuration) =>
        Matches(request, configuration.GetSection("Households:Internal")) || Matches(request, configuration.GetSection("Households:Internal:Devices"))
        || Matches(request, configuration.GetSection("Households:Internal:HomeMix"));

    private static bool Matches(HttpRequest request, IConfigurationSection client)
    {
        var clientId = client["ClientId"];
        var secret = client["ClientSecret"];
        return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(secret)
            && Same(clientId, request.Headers["X-Client-Id"].ToString()) && Same(secret, request.Headers["X-Client-Secret"].ToString());
    }

    private static bool Same(string expected, string given) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(given)));
}
