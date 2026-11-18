using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using Households.Api.Http;
using Households.Application.Assistant;
using Households.Application.Assistant.GetAssistant;
using Households.Application.Assistant.UpdateAssistant;
using Households.Domain.Households;
using MediatR;

namespace Households.Api.Endpoints;

internal sealed record AssistantRequest(
    bool Enabled, string? Name, AssistantAccessLevel AccessLevel, AssistantToolSources ToolSources, int EventDays, int LocationEventDays);

/// <summary>The household's JARVIS (not a member): everyone with access looks (D6), Admin and up change.</summary>
internal sealed class AssistantEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var assistant = app.MapGroup("/households/{id:guid}/assistant").WithTags("Assistant");

        assistant.MapGet("/", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAssistantQuery(id), ct)).ToHttpResult())
            .Produces<AssistantView>();

        assistant.MapPut("/", async (Guid id, AssistantRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateAssistantCommand(
                id, body.Enabled, body.Name, body.AccessLevel, body.ToolSources, body.EventDays, body.LocationEventDays), ct)).ToHttpResult())
            .Produces<AssistantView>()
            .RequireRateLimiting(RateLimits.Changes);
    }
}
