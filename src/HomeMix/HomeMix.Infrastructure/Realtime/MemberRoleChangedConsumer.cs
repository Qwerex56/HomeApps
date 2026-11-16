using HomeApps.Contracts.Households;
using HomeMix.Application.Realtime;
using MassTransit;

namespace HomeMix.Infrastructure.Realtime;

/// <summary>A new household role changes what the person may do in its add-ons: their open frames ask again.</summary>
public sealed class MemberRoleChangedConsumer(IRealtimeSender realtime) : IConsumer<MemberRoleChanged>
{
    public Task Consume(ConsumeContext<MemberRoleChanged> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var m = context.Message;
        return realtime.SendAsync([RealtimeEvents.PermissionsChanged(m.UserId, m.HouseholdId, m.NewRole.ToString())], context.CancellationToken);
    }
}
