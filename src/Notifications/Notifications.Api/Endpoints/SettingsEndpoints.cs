using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notifications.Api.Requests;
using Notifications.Application.Delivery;
using Notifications.Application.Delivery.SendTest;
using Notifications.Application.Mutes.Mute;
using Notifications.Application.Mutes.Unmute;
using Notifications.Application.Settings.ChangeSettings;
using Notifications.Application.Settings.GetSettings;

namespace Notifications.Api.Endpoints;

/// <summary>My settings (N3, N4), mutes per household and plugin, and a test notification. Always the caller from the token.</summary>
internal sealed class SettingsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var notifications = app.MapGroup("/notifications").WithTags("Settings");

        notifications.MapGet("/settings", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetSettingsQuery(), ct)).ToHttpResult())
            .Produces<NotificationSettingsView>();

        notifications.MapPut("/settings", async (ChangeSettingsRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ChangeSettingsCommand(body.ShowPreview, body.QuietHours), ct)).ToHttpResult());

        notifications.MapPut("/mutes", async (MuteRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new MuteCommand(body.HouseholdId, body.Source), ct)).ToHttpResult());

        notifications.MapDelete("/mutes", async ([FromQuery] Guid householdId, [FromQuery] string? source, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UnmuteCommand(householdId, source), ct)).ToHttpResult());

        notifications.MapPost("/test", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new SendTestCommand(), ct)).ToHttpResult())
            .Produces<DeliveryReport>();
    }
}
