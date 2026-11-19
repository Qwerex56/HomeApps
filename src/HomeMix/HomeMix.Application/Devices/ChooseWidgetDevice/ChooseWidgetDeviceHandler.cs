using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Installations;
using HomeMix.Application.Realtime;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Devices.ChooseWidgetDevice;

internal sealed class ChooseWidgetDeviceHandler(
    IInstallationRepository installations, IPackageRepository packages, InstallationAccess installationAccess, DevicesAccess access,
    IHouseholdDevices devices, RealtimeOutbox realtime)
    : ICommandHandler<ChooseWidgetDeviceCommand, InstallationView>
{
    public async Task<Result<InstallationView>> Handle(ChooseWidgetDeviceCommand command, CancellationToken cancellationToken)
    {
        var found = await WidgetSettings.FindAsync(command.InstallationId, installations, packages, installationAccess, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult();
        }

        var (installation, package) = found.Value;
        var opened = await access.OpenAsync(installation.Id, AddonCapabilities.Devices, cancellationToken);
        if (opened.IsFailed)
        {
            return opened.ToResult();
        }

        var widget = package.Manifest.Widgets.FirstOrDefault(w => w.Id == command.WidgetId && w.Devices.Count > 0);
        if (widget is null)
        {
            return Result.Fail(InstallationErrors.WidgetWithoutDevices(command.WidgetId));
        }

        if (command.DeviceId is { } deviceId)
        {
            var household = await devices.ListAsync(installation.TargetId, null, null, null, false, cancellationToken);
            var matches = DeviceMatcher.Match(widget.Devices, household.Select(d => d.ToCandidate()), null);
            if (!matches.Candidates.Any(c => c.Id == deviceId))
            {
                return Result.Fail(household.Any(d => d.Id == deviceId) ? DeviceErrors.NotAMatch(widget.Id) : DeviceErrors.NotFound);
            }
        }

        var picked = installation.PickDevice(package.Manifest, widget.Id, command.DeviceId);
        if (picked.IsFailed)
        {
            return picked;
        }

        realtime.Add(RealtimeEvents.InstallationChanged(installation, "widgets"));
        return InstallationView.From(installation, package);
    }
}
