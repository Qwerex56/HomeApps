using HomeApps.Contracts.Devices;
using MassTransit;

namespace HomeMix.Infrastructure.Triggers;

/// <summary>Runs of one household are sequential, so a retry or a burst never overlaps a script that is still setting devices.</summary>
public sealed class DeviceEventOccurredConsumerDefinition : ConsumerDefinition<DeviceEventOccurredConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<DeviceEventOccurredConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(consumerConfigurator);
        consumerConfigurator.Message<DeviceEventOccurred>(m => m.UsePartitioner(16, c => c.Message.HouseholdId));
    }
}
