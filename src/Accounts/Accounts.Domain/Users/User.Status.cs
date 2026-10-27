using Accounts.Domain.Common;
using Accounts.Domain.Users.Events;
using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>Disabling and enabling the account (UC-03, UC-05, UC-14). Accounts are never deleted in the normal flow (R5).</summary>
public sealed partial class User
{
    public bool IsDisabledBySelf => Status == AccountStatus.Disabled && Disabling?.ByUserId == Id;

    /// <summary>UC-03, UC-14: R1; the Owner's account cannot be disabled, not even by the Owner (R3). Optional reason.</summary>
    public Result Disable(Actor actor, string? reason, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        if (Role == ServerRole.Owner)
        {
            return Result.Fail(UserErrors.OwnerProtected);
        }

        if (!CanBeManagedBy(actor))
        {
            return Result.Fail(UserErrors.ForbiddenRole);
        }

        if (Status == AccountStatus.Disabled)
        {
            return Result.Fail(UserErrors.AlreadyDisabled);
        }

        var now = time.GetUtcNow();
        var disabling = Disabling.Create(now, actor.Id, reason);
        if (disabling.IsFailed)
        {
            return disabling.ToResult();
        }

        Status = AccountStatus.Disabled;
        Disabling = disabling.Value;
        Record(now, actor.Id, HistoryAction.Disabled, disabling.Value.Reason);
        _domainEvents.Add(new UserDisabled(Id, IsDisabledBySelf));
        return Result.Ok();
    }

    /// <summary>
    /// UC-14: a higher role enables any disabled account (R1), also one the user disabled. The user never enables their
    /// own account here: that is <see cref="ReactivateSelf"/>, after signing in (R8).
    /// </summary>
    public Result Enable(Actor actor, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        if (actor.Id == Id || !CanBeManagedBy(actor))
        {
            return Result.Fail(UserErrors.ForbiddenRole);
        }

        if (Status != AccountStatus.Disabled)
        {
            return Result.Fail(UserErrors.NotDisabled);
        }

        var now = time.GetUtcNow();
        Activate();
        Record(now, actor.Id, HistoryAction.Enabled);
        _domainEvents.Add(new UserEnabled(Id));
        return Result.Ok();
    }

    /// <summary>
    /// UC-05, R8: the user restores an account they disabled themselves, after a correct sign-in in Auth (the
    /// reactivation ticket proves it) and checking their data. <paramref name="checkedProfile"/> is the profile from the
    /// form, applied in the same step when changed. An account disabled by a higher role stays disabled.
    /// </summary>
    public Result ReactivateSelf(Profile? checkedProfile, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (Status != AccountStatus.Disabled)
        {
            return Result.Fail(UserErrors.NotDisabled);
        }

        if (!IsDisabledBySelf)
        {
            return Result.Fail(UserErrors.ReactivationNotAllowed);
        }

        var now = time.GetUtcNow();
        if (checkedProfile is not null)
        {
            ApplyProfile(Id, checkedProfile, now);
        }

        Activate();
        Record(now, Id, HistoryAction.ReactivatedBySelf);
        _domainEvents.Add(new UserEnabled(Id));
        return Result.Ok();
    }

    private void Activate()
    {
        Status = AccountStatus.Active;
        Disabling = null;
    }
}
