using HomeApps.Contracts.Devices;
using Jarvis.Application.Accounts.ForgetUser;
using MassTransit;
using MediatR;

namespace Jarvis.Infrastructure.Messaging;

/// <summary>A satellite left its household (or got a new pairing code): what it heard and its runs go.</summary>
public sealed class HouseholdDeviceUnpairedConsumer(ISender sender) : IConsumer<HouseholdDeviceUnpaired>
{
    public async Task Consume(ConsumeContext<HouseholdDeviceUnpaired> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await sender.Send(new ForgetUserCommand(context.Message.DeviceId), context.CancellationToken);
    }
}
