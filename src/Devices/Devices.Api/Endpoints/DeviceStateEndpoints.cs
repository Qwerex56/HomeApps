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
using HomeApps.BuildingBlocks.Api.Assistant;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Devices.Api.Endpoints;

/// <summary>
/// What household devices report and what members do with it (household-jarvis-devices.md 2.3, D6, D9): last state, event
/// history, <c>set</c>, a stream (SSE) of changes, and <c>POST /devices/report</c> for devices that sleep (the device's own token).
/// </summary>
internal sealed class DeviceStateEndpoints : IEndpoint
{
    private const int MaxReportBytes = 64 * 1024;
    /// <summary>Configuration key that overrides <see cref="MaxLifetime"/> (tests use a short one).</summary>
    internal const string LifetimeSetting = "Devices:StreamLifetime";

    internal static readonly TimeSpan MaxLifetime = TimeSpan.FromMinutes(15);
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
        {
            try
            {
                return (await sender.Send(new ProcessFramesCommand(user.RequiredUserId, frames), ct)).ToHttpResult();
            }
            catch (Exception exception) when (exception is DbUpdateException or ConcurrencyConflictException)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "The report could not be saved",
                    detail: "The device's data changed in the meantime or could not be stored. Send the report again.",
                    extensions: new Dictionary<string, object?> { [ResultHttpMapper.CodeExtension] = "devices.frame_failed" });
            }
        })
            .AllowAssistant() // a device's token carries `act` (the device actor), so the guard would otherwise refuse this write
            .RequireRateLimiting(DependencyInjection.ReportRateLimit)
            .WithMetadata(new RequestSizeLimitAttribute(MaxReportBytes))
            .WithTags("Devices")
            .Produces<IReadOnlyList<FrameResult>>();

        app.MapGet("/devices/households/{householdId:guid}/stream", async (Guid householdId, ISender sender, DeviceStream stream, IConfiguration configuration, CancellationToken ct) =>
        {
            var access = await sender.Send(new GetStreamAccessQuery(householdId), ct);
            return access.IsFailed ? access.ToHttpResult() : TypedResults.ServerSentEvents(ReadAsync(stream, householdId, access.Value, configuration.GetValue<TimeSpan?>(LifetimeSetting) ?? MaxLifetime, ct));
        }).WithTags("Devices");
    }

    private static async IAsyncEnumerable<SseItem<string>> ReadAsync(
        DeviceStream stream, Guid householdId, bool access, TimeSpan maxLifetime, [EnumeratorCancellation] CancellationToken requestAborted)
    {
        // The stream ends after MaxLifetime so the client reconnects and its access is checked again.
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        lifetime.CancelAfter(maxLifetime);
        var cancellationToken = lifetime.Token;
        using var subscription = stream.Subscribe(householdId, access);
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
                catch (OperationCanceledException) when (!requestAborted.IsCancellationRequested)
                {
                    // The lifetime ended while waiting: the stream ends cleanly below.
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                yield return message is null
                    ? new SseItem<string>("{}", "ping")
                    : new SseItem<string>(JsonSerializer.Serialize(message.Data, Json), message.Type);
            }
        }
    }
}
