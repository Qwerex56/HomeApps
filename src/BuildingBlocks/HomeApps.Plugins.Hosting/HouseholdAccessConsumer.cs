using HomeApps.Contracts.Households;
using MassTransit;

namespace HomeApps.Plugins.Hosting;

/// <summary>
/// Forgets cached roles as soon as Households reports a change. One queue per plugin (<c>{id}-household-access</c>),
/// so with several copies of one plugin running only one of them hears it; the others catch up when their cache expires.
/// Public only because MassTransit's assembly scan finds public consumers alone.
/// </summary>
public sealed class HouseholdAccessConsumer(HouseholdAccessCache cache) :
    IConsumer<MemberJoined>,
    IConsumer<MemberLeft>,
    IConsumer<MemberRoleChanged>,
    IConsumer<OwnershipTransferred>,
    IConsumer<HouseholdArchived>,
    IConsumer<HouseholdRestored>
{
    public Task Consume(ConsumeContext<MemberJoined> context) => Forget(context.Message.HouseholdId, context.Message.UserId);

    public Task Consume(ConsumeContext<MemberLeft> context) => Forget(context.Message.HouseholdId, context.Message.UserId);

    public Task Consume(ConsumeContext<MemberRoleChanged> context) => Forget(context.Message.HouseholdId, context.Message.UserId);

    public Task Consume(ConsumeContext<OwnershipTransferred> context) => Forget(context.Message.HouseholdId);

    public Task Consume(ConsumeContext<HouseholdArchived> context) => Forget(context.Message.HouseholdId);

    public Task Consume(ConsumeContext<HouseholdRestored> context) => Forget(context.Message.HouseholdId);

    private Task Forget(Guid householdId, Guid? userId = null)
    {
        if (userId is { } id)
        {
            cache.Forget(householdId, id);
        }
        else
        {
            cache.Forget(householdId);
        }

        return Task.CompletedTask;
    }
}
