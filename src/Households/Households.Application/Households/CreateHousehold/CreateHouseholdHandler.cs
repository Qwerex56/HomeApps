using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

internal sealed class CreateHouseholdHandler(
    IHouseholdRepository households, IUserCopies users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<CreateHouseholdCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateHouseholdCommand command, CancellationToken cancellationToken)
    {
        var active = await users.EnsureActiveAsync(currentUser.RequiredUserId, cancellationToken);
        if (active.IsFailed)
        {
            return active.ToResult<Guid>();
        }

        var household = Household.Create(Guid.CreateVersion7(time.GetUtcNow()), currentUser.RequiredUserId, command.Name, command.Description, time);
        if (household.IsFailed)
        {
            return household.ToResult<Guid>();
        }

        households.Add(household.Value);
        return household.Value.Id;
    }
}
