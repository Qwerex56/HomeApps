using System.Text.Json;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Devices.GetInstallationDeviceState;

internal sealed class GetInstallationDeviceStateHandler(DevicesAccess access, IHouseholdDevices devices)
    : IQueryHandler<GetInstallationDeviceStateQuery, JsonElement>
{
    public async Task<Result<JsonElement>> Handle(GetInstallationDeviceStateQuery query, CancellationToken cancellationToken)
    {
        var opened = await access.OpenAsync(query.InstallationId, AddonCapabilities.Devices, cancellationToken);
        if (opened.IsFailed)
        {
            return opened.ToResult();
        }

        var inHousehold = await access.EnsureInHouseholdAsync(opened.Value, query.DeviceId, cancellationToken);
        if (inHousehold.IsFailed)
        {
            return inHousehold;
        }

        var state = await devices.GetStateAsync(query.DeviceId, cancellationToken);
        return state is { } value ? value : Result.Fail(DeviceErrors.NotFound);
    }
}
