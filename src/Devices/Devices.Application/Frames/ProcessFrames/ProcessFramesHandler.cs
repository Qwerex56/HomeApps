using System.Globalization;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Contracts.Devices;

namespace Devices.Application.Frames.ProcessFrames;

/// <summary>
/// Applies a device's frames in order, each checked against its own descriptor: <c>state</c> replaces last values,
/// <c>event</c> is recorded and published, <c>status</c> is the heartbeat (it ends silence and may tell admins about a low
/// battery). A refused frame is reported in its place in the answer and changes nothing; the others still count.
/// </summary>
internal sealed class ProcessFramesHandler(
    IDeviceRepository devices, IDeviceTelemetry telemetry, IDeviceStream stream, IEventBus bus, AdminNotices notices, IControlSources sources, TimeProvider time)
    : ICommandHandler<ProcessFramesCommand, IReadOnlyList<FrameResult>>
{
    public async Task<Result<IReadOnlyList<FrameResult>>> Handle(ProcessFramesCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindAsync(command.DeviceId, cancellationToken);
        if (device is not { IsPairedSatellite: true, IsDisabled: false, Descriptor: not null }
            || DescriptorParser.Parse(device.Descriptor) is not { IsSuccess: true } parsed)
        {
            return Result.Fail(DeviceErrors.FramesUnsupported);
        }

        var now = time.GetUtcNow();
        var state = await telemetry.FindStateAsync(device.Id, cancellationToken);
        if (state is null)
        {
            state = DeviceState.For(device.Id);
            telemetry.Add(state);
        }

        var results = new List<FrameResult>(command.Frames.Count);
        foreach (var frame in command.Frames)
        {
            var applied = frame.Type switch
            {
                "state" => await ApplyStateAsync(device, state, parsed.Value, frame, now, cancellationToken),
                "event" => await ApplyEventAsync(device, parsed.Value, frame, now, cancellationToken),
                "status" => await ApplyStatusAsync(device, state, frame, now, cancellationToken),
                _ => Result.Fail(DeviceErrors.UnknownFrame(frame.Type)),
            };
            results.Add(applied.IsSuccess ? new FrameResult(true) : Refused(applied.Errors[0]));
        }

        if (results.Exists(r => r.Ok))
        {
            device.Seen(now);
        }

        return results;
    }

    private async Task<Result> ApplyStateAsync(
        Device device, DeviceState state, DeviceDescriptor descriptor, DeviceFrame frame, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var checkedValues = FrameValidation.CheckState(descriptor, frame.Values);
        if (checkedValues.IsFailed)
        {
            return checkedValues;
        }

        var changed = state.ApplyValues(frame.Values!.Value, now);
        stream.Publish(new DeviceStreamMessage(device.HouseholdId!.Value, "state", new { deviceId = device.Id, values = frame.Values.Value, at = now }, MembersOnly: device.IsLocationDevice));
        if (changed is not null)
        {
            using var keys = System.Text.Json.JsonDocument.Parse(changed);
            var changedBy = sources.Find(device.Id, keys.RootElement.EnumerateObject().Select(p => p.Name));
            await bus.PublishAsync(
                new DeviceStateChanged(device.HouseholdId.Value, device.Id, device.Slug, changed, now, changedBy, Info(device)), cancellationToken);
        }

        return Result.Ok();
    }

    private async Task<Result> ApplyEventAsync(Device device, DeviceDescriptor descriptor, DeviceFrame frame, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var declared = FrameValidation.CheckEvent(descriptor, frame.Name, frame.Data);
        if (declared.IsFailed)
        {
            return declared.ToResult();
        }

        // The device's clock may be wrong or unset: a time that is missing, unreadable, in the future or older than 366 days becomes now.
        var at = DateTimeOffset.TryParse(frame.At, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedAt) && parsedAt <= now && parsedAt >= now - EventRetention.MaxEventAge ? parsedAt : now;
        var data = frame.Data is { ValueKind: not System.Text.Json.JsonValueKind.Null } payload ? payload.GetRawText() : null;
        var householdId = device.HouseholdId!.Value;
        var isLocation = descriptor.IsLocation(declared.Value);

        telemetry.Add(DeviceEvent.Record(device.Id, householdId, declared.Value.Name, data, at, isLocation));
        await bus.PublishAsync(new DeviceEventOccurred(householdId, device.Id, device.Slug, declared.Value.Name, data, at, isLocation, Info(device)), cancellationToken);
        stream.Publish(new DeviceStreamMessage(
            householdId, "event", new { deviceId = device.Id, name = declared.Value.Name, data = frame.Data, at, isLocation }, MembersOnly: true));
        return Result.Ok();
    }

    private async Task<Result> ApplyStatusAsync(Device device, DeviceState state, DeviceFrame frame, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var report = new StatusReport(frame.Battery, frame.Voltage, frame.Rssi, frame.Uptime, frame.Firmware, frame.Error);
        var valid = report.Check();
        if (valid.IsFailed)
        {
            return valid;
        }

        var outcome = state.ApplyStatus(report, now);
        stream.Publish(new DeviceStreamMessage(
            device.HouseholdId!.Value, "status",
            new { deviceId = device.Id, silent = false, lowBattery = state.LowBattery, battery = state.Battery, voltage = state.Voltage, rssi = state.Rssi, uptime = state.Uptime, firmware = state.Firmware, error = state.Error, at = now }));
        if (outcome.Returned)
        {
            await notices.SendAsync(device, "silent", "Urządzenie znów zgłasza stan.", cancellationToken);
        }

        if (outcome.BatteryLow)
        {
            await notices.SendAsync(device, "battery", $"Niski poziom baterii: {state.Battery:0}%.", cancellationToken);
        }

        return Result.Ok();
    }

    private static DeviceInfo Info(Device device) => new(device.Name, device.Model, device.Roles, device.Capabilities);

    private static FrameResult Refused(IError error) =>
        error is AppError app ? new FrameResult(false, app.Code, app.Message) : new FrameResult(false, "devices.invalid_frame", error.Message);
}
