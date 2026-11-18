using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace Devices.Application.Common;

/// <summary>
/// Tells a household's admins (Admin and Owner) about a device through Notifications. When Households cannot be asked, the
/// notice is dropped and logged: the state change it reports is not repeated.
/// </summary>
internal sealed partial class AdminNotices(IHouseholdDirectory households, IEventBus bus, ILogger<AdminNotices> logger)
{
    public async Task SendAsync(Device device, string category, string body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(device);
        var householdId = device.HouseholdId!.Value;
        IReadOnlyList<Guid> admins;
        try
        {
            admins = await households.GetAdminIdsAsync(householdId, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogAdminsUnavailable(householdId, exception);
            return;
        }

        if (admins.Count > 0)
        {
            await bus.PublishAsync(
                new NotificationRequested(admins, "devices", category, device.Name, body, $"/homes/{householdId}/devices", householdId, Tag: $"{category}-{device.Id}"),
                cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The admins of household {HouseholdId} could not be asked; the device notice is dropped.")]
    private partial void LogAdminsUnavailable(Guid householdId, Exception exception);
}
