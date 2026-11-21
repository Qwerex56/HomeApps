using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Devices;

namespace Devices.Application.Heartbeat.CheckHeartbeats;

/// <summary>
/// A smart_home or assistant device owes a status report every heartbeat period; after 2.5 periods without one it is
/// silent: <see cref="DeviceWentSilent"/> is published and the household's admins are told, once, until it reports again.
/// Returns how many devices went silent in this scan.
/// </summary>
internal sealed class CheckHeartbeatsHandler(
    IDeviceRepository devices, IDeviceTelemetry telemetry, IDeviceStream stream, IEventBus bus, AdminNotices notices, TimeProvider time)
    : ICommandHandler<CheckHeartbeatsCommand, int>
{
    public async Task<Result<int>> Handle(CheckHeartbeatsCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var candidates = await devices.HeartbeatCandidatesAsync(cancellationToken);
        var states = await telemetry.StatesAsync([.. candidates.Select(d => d.Id)], cancellationToken);
        var silent = 0;
        foreach (var device in candidates)
        {
            if (DescriptorParser.Parse(device.Descriptor) is not { IsSuccess: true } descriptor)
            {
                continue;
            }

            if (!states.TryGetValue(device.Id, out var state))
            {
                state = DeviceState.For(device.Id);
                telemetry.Add(state);
            }

            var reference = device.PairedAt ?? device.CreatedAt;
            if (!state.GoSilent(reference > command.NotBefore ? reference : command.NotBefore, TimeSpan.FromSeconds(descriptor.Value.HeartbeatSeconds), now))
            {
                continue;
            }

            silent++;
            var householdId = device.HouseholdId!.Value;
            await bus.PublishAsync(new DeviceWentSilent(householdId, device.Id, device.Name, state.LastStatusAt), cancellationToken);
            await notices.SendAsync(device, "silent", "Urządzenie przestało zgłaszać stan.", cancellationToken);
            stream.Publish(new DeviceStreamMessage(householdId, "status", new { deviceId = device.Id, silent = true, lowBattery = state.LowBattery, at = now }));
        }

        return silent;
    }
}
