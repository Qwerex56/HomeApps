using Households.Application.DevicesSync.RemoveDeviceMember;
using HomeApps.Contracts.Devices;
using MassTransit;
using MediatR;

namespace Households.Infrastructure.Devices;

/// <summary>S13: a removed satellite, or one given a new code, leaves its household.</summary>
public sealed class HouseholdDeviceUnpairedConsumer(ISender sender) : IConsumer<HouseholdDeviceUnpaired>
{
    public Task Consume(ConsumeContext<HouseholdDeviceUnpaired> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var m = context.Message;
        return sender.Send(new RemoveDeviceMemberCommand(m.DeviceId, m.HouseholdId), context.CancellationToken);
    }
}
