using Auth.Application.DevicesSync.UnpairDevice;
using HomeApps.Contracts.Devices;
using MassTransit;
using MediatR;

namespace Auth.Infrastructure.Devices;

/// <summary>jarvis-satellites.md 3a: a removed satellite, or one given a new code, loses its secret.</summary>
public sealed class HouseholdDeviceUnpairedConsumer(ISender sender) : IConsumer<HouseholdDeviceUnpaired>
{
    public Task Consume(ConsumeContext<HouseholdDeviceUnpaired> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return sender.Send(new UnpairDeviceCommand(context.Message.DeviceId), context.CancellationToken);
    }
}
