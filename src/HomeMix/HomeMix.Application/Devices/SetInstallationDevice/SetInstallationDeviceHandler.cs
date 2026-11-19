using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Devices.SetInstallationDevice;

internal sealed class SetInstallationDeviceHandler(DevicesAccess access, IHouseholdDevices devices) : ICommandHandler<SetInstallationDeviceCommand>
{
    public async Task<Result> Handle(SetInstallationDeviceCommand command, CancellationToken cancellationToken)
    {
        var opened = await access.OpenAsync(command.InstallationId, AddonCapabilities.DevicesControl, cancellationToken);
        if (opened.IsFailed)
        {
            return opened.ToResult();
        }

        var inHousehold = await access.EnsureInHouseholdAsync(opened.Value, command.DeviceId, cancellationToken);
        return inHousehold.IsFailed ? inHousehold : await devices.SetAsync(command.DeviceId, command.Control, command.Value, cancellationToken);
    }
}
