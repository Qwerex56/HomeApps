using System.Runtime.CompilerServices;
using System.Net.ServerSentEvents;
using System.Text.Json;
using Devices.Api.Requests;
using Devices.Api.Stream;
using Devices.Application.Common;
using Devices.Application.Controls.SetControl;
using Devices.Application.Frames;
using Devices.Application.Frames.ProcessFrames;
using Devices.Application.Telemetry.GetDeviceEvents;
using Devices.Application.Telemetry.GetDeviceState;
using Devices.Application.Telemetry.GetStreamAccess;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Devices.Api.Endpoints;

/// <summary>
/// What household devices report and what members do with it (household-jarvis-devices.md 2.3, D6, D9): last state, event
/// history, <c>set</c>, a stream (SSE) of changes, and <c>POST /devices/report</c> for devices that sleep (the device's own token).
/// </summary>
internal sealed class DeviceStateEndpoints : IEndpoint
{
    private const int MaxReportBytes = 64 * 1024;
    private static readonly TimeSpan KeepAlive = TimeSpan.FromSeconds(20);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/devices/{deviceId:guid}/state", async (Guid deviceId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetDeviceStateQuery(deviceId), ct)).ToHttpResult())
            .WithTags("Devices")
            .Produces<DeviceStateView>();

        app.MapGet("/devices/{deviceId:guid}/events", async (
                Guid deviceId, ISender sender, CancellationToken ct, int skip = 0, int limit = GetDeviceEventsQuery.DefaultLimit) =>
            (await sender.Send(new GetDeviceEventsQuery(deviceId, skip, limit), ct)).ToHttpResult())
            .WithTags("Devices")
            .Produces<IReadOnlyList<DeviceEventView>>();

        app.MapPost("/devices/{deviceId:guid}/set", async (Guid deviceId, SetControlRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetControlCommand(deviceId, body.Control, body.Value), ct)).ToHttpResult())
            .WithTags("Devices");

        // The device's own token (its subject is the device); the frames are the same as on /devices/link.
        app.MapPost("/devices/report", async (List<DeviceFrame> frames, ICurrentUser user, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ProcessFramesCommand(user.RequiredUserId, frames), ct)).ToHttpResult())
            .WithMetadata(new RequestSizeLimitAttribute(MaxReportBytes))
            .WithTags("Devices")
            .Produces<IReadOnlyList<FrameResult>>();

        app.MapGet("/devices/households/{householdId:guid}/stream", async (Guid householdId, ISender sender, DeviceStream stream, CancellationToken ct) =>
        {
            var access = await sender.Send(new GetStreamAccessQuery(householdId), ct);
            return access.IsFailed ? access.ToHttpResult() : TypedResults.ServerSentEvents(ReadAsync(stream.Subscribe(householdId, access.Value), ct));
        }).WithTags("Devices");
    }

    private static async IAsyncEnumerable<SseItem<string>> ReadAsync(StreamSubscription subscription, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using (subscription)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                wait.CancelAfter(KeepAlive);
                Application.Abstractions.DeviceStreamMessage? message = null;
                try
                {
                    message = await subscription.Reader.ReadAsync(wait.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Nothing happened for a while: a ping keeps proxies from closing the stream.
                }

                yield return message is null
                    ? new SseItem<string>("{}", "ping")
                    : new SseItem<string>(JsonSerializer.Serialize(message.Data, Json), message.Type);
            }
        }
    }
}
