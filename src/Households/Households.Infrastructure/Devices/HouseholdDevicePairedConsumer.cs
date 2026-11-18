using Households.Application.DevicesSync.AddDeviceMember;
using Households.Infrastructure.Accounts;
using HomeApps.Contracts.Devices;
using MassTransit;
using MediatR;

namespace Households.Infrastructure.Devices;

/// <summary>jarvis-satellites.md 3a (S13): a paired satellite joins its household as a Member.</summary>
public sealed class HouseholdDevicePairedConsumer(ISender sender) : IConsumer<HouseholdDevicePaired>
{
    public async Task Consume(ConsumeContext<HouseholdDevicePaired> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var m = context.Message;
        (await sender.Send(new AddDeviceMemberCommand(m.DeviceId, m.HouseholdId, m.Name), context.CancellationToken)).ThrowIfFailed(m.DeviceId);
    }
}
