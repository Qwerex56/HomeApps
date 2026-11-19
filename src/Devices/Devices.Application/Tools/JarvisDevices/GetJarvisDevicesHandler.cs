using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Households;
using Microsoft.Extensions.Logging;

namespace Devices.Application.Tools.JarvisDevices;

internal sealed partial class GetJarvisDevicesHandler(IDeviceRepository devices, IDeviceTelemetry telemetry, IDeviceLinks links, IHouseholdDirectory directory, ILogger<GetJarvisDevicesHandler> logger)
    : IQueryHandler<GetJarvisDevicesQuery, IReadOnlyList<DeviceDescriptionView>>
{
    public async Task<Result<IReadOnlyList<DeviceDescriptionView>>> Handle(GetJarvisDevicesQuery query, CancellationToken cancellationToken)
    {
        var all = (await devices.OfHouseholdsAsync([query.HouseholdId], cancellationToken)).Where(d => !d.IsDisabled).ToList();
        // A satellite (the household's own device) has no person's role here: it is treated as a guest, so location values stay hidden.
        HouseholdRole? role = null;
        if (!all.Any(d => d.Id == query.UserId))
        {
            try
            {
                role = await directory.GetRoleAsync(query.HouseholdId, query.UserId, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                LogMembersUnavailable(query.HouseholdId, exception);
                return Result.Fail(DeviceErrors.MembersUnavailable);
            }

            if (role is null)
            {
                return Result.Ok<IReadOnlyList<DeviceDescriptionView>>([]);
            }
        }

        var found = ReachableDevices.Name(all.Where(GeneratedTools.Applies));
        var states = await telemetry.StatesAsync([.. found.Select(r => r.Device.Id)], cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceDescriptionView>>(
            [.. found.OrderBy(r => r.Device.Name, StringComparer.Ordinal)
                .Select(r => DeviceDescriptionView.From(r, states.GetValueOrDefault(r.Device.Id), links.IsOnline(r.Device.Id), role))]);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The members of household {HouseholdId} could not be asked; the device list is not given.")]
    private partial void LogMembersUnavailable(Guid householdId, Exception exception);
}
