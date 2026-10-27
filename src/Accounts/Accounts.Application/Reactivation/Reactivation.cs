using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Reactivation;

/// <summary>
/// UC-05, R8: the profile to check before restoring an account the user disabled. The caller is identified by the
/// reactivation ticket from Auth (the Api accepts only the ticket here), so <see cref="ICurrentUser"/> works as usual.
/// </summary>
public sealed record GetReactivationFormQuery : IQuery<ProfileView>;

internal sealed class GetReactivationFormHandler(IUserRepository users, ICurrentUser currentUser)
    : IQueryHandler<GetReactivationFormQuery, ProfileView>
{
    public async Task<Result<ProfileView>> Handle(GetReactivationFormQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail<ProfileView>(ApplicationErrors.UserNotFound);
        }

        if (user.Status != AccountStatus.Disabled)
        {
            return Result.Fail<ProfileView>(UserErrors.NotDisabled);
        }

        return user.IsDisabledBySelf ? user.Profile.ToView() : Result.Fail<ProfileView>(UserErrors.ReactivationNotAllowed);
    }
}

/// <summary>UC-05, R8: restore with the checked (possibly corrected) profile. Auth gets <c>UserEnabled</c> and issues tokens.</summary>
public sealed record ReactivateMyAccountCommand(ProfileInput Profile) : ICommand;

internal sealed class ReactivateMyAccountValidator : AbstractValidator<ReactivateMyAccountCommand>
{
    public ReactivateMyAccountValidator() => RuleFor(c => c.Profile).NotNull().SetValidator(new ProfileInputValidator());
}

internal sealed class ReactivateMyAccountHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<ReactivateMyAccountCommand>
{
    public async Task<Result> Handle(ReactivateMyAccountCommand command, CancellationToken cancellationToken)
    {
        var profile = command.Profile.ToProfile(time);
        if (profile.IsFailed)
        {
            return profile.ToResult();
        }

        var user = await users.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        return user is null
            ? Result.Fail(ApplicationErrors.UserNotFound)
            : user.ReactivateSelf(profile.Value, time);
    }
}
