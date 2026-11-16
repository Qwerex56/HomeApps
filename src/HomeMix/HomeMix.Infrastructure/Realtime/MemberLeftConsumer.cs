using HomeApps.Contracts.Households;
using HomeMix.Application.Realtime;
using MassTransit;

namespace HomeMix.Infrastructure.Realtime;

/// <summary>Out of the household: the person's open frames of its add-ons lose their permissions (role null).</summary>
public sealed class MemberLeftConsumer(IRealtimeSender realtime) : IConsumer<MemberLeft>
{
    public Task Consume(ConsumeContext<MemberLeft> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var m = context.Message;
        return realtime.SendAsync([RealtimeEvents.PermissionsChanged(m.UserId, m.HouseholdId, null)], context.CancellationToken);
    }
}
