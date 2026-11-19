using HomeApps.Contracts.Devices;
using MassTransit;

namespace HomeMix.Infrastructure.Triggers;

/// <summary>Runs of one household are sequential, so a retry or a burst never overlaps a script that is still setting devices.</summary>
public sealed class DeviceStateChangedConsumerDefinition : ConsumerDefinition<DeviceStateChangedConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<DeviceStateChangedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(consumerConfigurator);
        consumerConfigurator.Message<DeviceStateChanged>(m => m.UsePartitioner(16, c => c.Message.HouseholdId));
    }
}
