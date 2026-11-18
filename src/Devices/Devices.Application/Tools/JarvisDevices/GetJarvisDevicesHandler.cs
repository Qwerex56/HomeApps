using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Logging;

namespace Devices.Application.Tools.JarvisDevices;

internal sealed partial class GetJarvisDevicesHandler(IDeviceRepository devices, IDeviceTelemetry telemetry, IDeviceLinks links, IHouseholdDirectory directory, ILogger<GetJarvisDevicesHandler> logger)
    : IQueryHandler<GetJarvisDevicesQuery, IReadOnlyList<DeviceDescriptionView>>
{
    public async Task<Result<IReadOnlyList<DeviceDescriptionView>>> Handle(GetJarvisDevicesQuery query, CancellationToken cancellationToken)
    {
        var all = (await devices.OfHouseholdsAsync([query.HouseholdId], cancellationToken)).Where(d => !d.IsDisabled).ToList();
        if (!all.Any(d => d.Id == query.UserId))
        {
            IReadOnlyList<Guid> members;
            try
            {
                members = await directory.GetMemberIdsAsync(query.HouseholdId, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                LogMembersUnavailable(query.HouseholdId, exception);
                return Result.Fail(DeviceErrors.MembersUnavailable);
            }

            if (!members.Contains(query.UserId))
            {
                return Result.Ok<IReadOnlyList<DeviceDescriptionView>>([]);
            }
        }

        var found = ReachableDevices.Name(all.Where(GeneratedTools.Applies));
        var states = await telemetry.StatesAsync([.. found.Select(r => r.Device.Id)], cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceDescriptionView>>(
            [.. found.OrderBy(r => r.Device.Name, StringComparer.Ordinal)
                .Select(r => DeviceDescriptionView.From(r, states.GetValueOrDefault(r.Device.Id), links.IsOnline(r.Device.Id)))]);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The members of household {HouseholdId} could not be asked; the device list is not given.")]
    private partial void LogMembersUnavailable(Guid householdId, Exception exception);
}
