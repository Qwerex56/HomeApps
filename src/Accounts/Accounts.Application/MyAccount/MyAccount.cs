using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

/// <summary>UC-01: my account. Missing right after sign-up while Auth's event is on its way: 404 <c>user.pending</c>, retry.</summary>
public sealed record GetMyAccountQuery : IQuery<MyAccountView>;

internal sealed class GetMyAccountHandler(IUserRepository users, ICurrentUser currentUser) : IQueryHandler<GetMyAccountQuery, MyAccountView>
{
    public async Task<Result<MyAccountView>> Handle(GetMyAccountQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        return user is null ? Result.Fail<MyAccountView>(ApplicationErrors.UserPending) : user.ToMyView();
    }
}

/// <summary>UC-02: the whole profile form (Q5). Email and password are changed in Auth.</summary>
public sealed record UpdateMyProfileCommand(ProfileInput Profile) : ICommand;

internal sealed class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileValidator() => RuleFor(c => c.Profile).NotNull().SetValidator(new ProfileInputValidator());
}

internal sealed class UpdateMyProfileHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<UpdateMyProfileCommand>
{
    public async Task<Result> Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = command.Profile.ToProfile(time);
        if (profile.IsFailed)
        {
            return profile.ToResult();
        }

        var user = await users.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        return user is null
            ? Result.Fail(ApplicationErrors.UserPending)
            : user.UpdateProfile(currentUser.Actor(), profile.Value, time);
    }
}

/// <summary>UC-03: I disable my account. Signing in again restores it (UC-05).</summary>
public sealed record DisableMyAccountCommand(string? Reason) : ICommand;

internal sealed class DisableMyAccountHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<DisableMyAccountCommand>
{
    public async Task<Result> Handle(DisableMyAccountCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        return user is null
            ? Result.Fail(ApplicationErrors.UserPending)
            : user.Disable(currentUser.Actor(), command.Reason, time);
    }
}

/// <summary>UC-04, Q2, Q6: when and what kind of change, newest first, at most <see cref="Limit"/>. No authors, no fields.</summary>
public sealed record GetMyHistoryQuery : IQuery<IReadOnlyList<MyHistoryEntryView>>
{
    public const int Limit = 50;
}

internal sealed class GetMyHistoryHandler(IUserDirectory directory, ICurrentUser currentUser)
    : IQueryHandler<GetMyHistoryQuery, IReadOnlyList<MyHistoryEntryView>>
{
    public async Task<Result<IReadOnlyList<MyHistoryEntryView>>> Handle(GetMyHistoryQuery query, CancellationToken cancellationToken)
    {
        var entries = await directory.HistoryAsync(currentUser.RequiredUserId, null, GetMyHistoryQuery.Limit, cancellationToken);
        return entries.Select(e => new MyHistoryEntryView(e.At, e.Action)).ToList();
    }
}
