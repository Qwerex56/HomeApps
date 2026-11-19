using System.Text.Json;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Logging;

namespace Devices.Application.Controls.SetControlForAddon;

/// <summary>
/// Checks, in this order: the device is a usable device of that household (JARVIS's per-device switch does not apply:
/// add-ons are governed by their consent), the installer is Admin or Owner there now, and the value fits the control.
/// Then it sends the set, remembers the source for the loop guard and writes the history entry.
/// </summary>
internal sealed partial class SetControlForAddonHandler(
    IDeviceRepository devices, IDeviceLinks links, IHouseholdDirectory households, IControlSources sources, IDeviceTelemetry telemetry,
    TimeProvider time, ILogger<SetControlForAddonHandler> logger)
    : ICommandHandler<SetControlForAddonCommand>
{
    public const string SourcePrefix = "homemix:";
    public const string HistoryEvent = "set_by_addon";

    public async Task<Result> Handle(SetControlForAddonCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindAsync(command.DeviceId, cancellationToken);
        if (device?.HouseholdId != command.HouseholdId || device.IsDisabled)
        {
            return Result.Fail(DeviceErrors.NotFound);
        }

        IReadOnlyList<Guid> admins;
        try
        {
            admins = await households.GetAdminIdsAsync(command.HouseholdId, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LogAdminsUnavailable(command.HouseholdId, exception);
            return Result.Fail(DeviceErrors.MembersUnavailable);
        }

        if (!admins.Contains(command.InstallerUserId))
        {
            return Result.Fail(DeviceErrors.Forbidden);
        }

        if (device.Descriptor is null || DescriptorParser.Parse(device.Descriptor) is not { IsSuccess: true } descriptor)
        {
            return Result.Fail(DeviceErrors.FramesUnsupported);
        }

        var valid = FrameValidation.CheckSet(descriptor.Value, command.Control, command.Value);
        if (valid.IsFailed)
        {
            return valid;
        }

        var source = SourcePrefix + command.InstallationId;
        // Remembered before the send: the device may report the new state before the answer reaches us.
        sources.Remember(device.Id, command.Control!, source);
        var set = await links.SetAsync(device.Id, command.Control!, command.Value!.Value, cancellationToken);
        if (set.IsFailed)
        {
            return set;
        }

        var data = JsonSerializer.Serialize(new
        {
            control = command.Control,
            value = command.Value,
            by = "addon",
            addon = command.AddonName,
            installationId = command.InstallationId,
            userId = command.InstallerUserId,
            text = $"ustawione przez dodatek {command.AddonName}",
        });
        telemetry.Add(DeviceEvent.Record(device.Id, command.HouseholdId, HistoryEvent, data, time.GetUtcNow(), isLocation: false));
        return Result.Ok();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The admins of household {HouseholdId} could not be asked; an add-on's set is refused.")]
    private partial void LogAdminsUnavailable(Guid householdId, Exception exception);
}
