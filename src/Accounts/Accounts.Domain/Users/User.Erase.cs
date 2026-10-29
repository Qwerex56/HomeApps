using Accounts.Domain.Common;
using Accounts.Domain.Users.Events;
using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>The rare "forget me" erase (UC-17, R5) and the copies kept in sync with Auth (UC-07, A-23, A-35).</summary>
public sealed partial class User
{
    /// <summary>
    /// UC-17, R5: only the Owner, only an already disabled account, never the Owner's own (R3), confirmed by typing the
    /// account's email (Q1). Returns the anonymous trace to store; the caller removes this user with its history.
    /// </summary>
    public Result<ErasedUser> Erase(Actor actor, string? confirmEmail, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        if (actor.Role != ServerRole.Owner)
        {
            return Result.Fail<ErasedUser>(UserErrors.ForbiddenRole);
        }

        if (Role == ServerRole.Owner)
        {
            return Result.Fail<ErasedUser>(UserErrors.OwnerProtected);
        }

        if (Status != AccountStatus.Disabled)
        {
            return Result.Fail<ErasedUser>(UserErrors.EraseNotDisabled);
        }

        var confirmed = Email.Create(confirmEmail);
        if (confirmed.IsFailed || confirmed.Value != Email)
        {
            return Result.Fail<ErasedUser>(UserErrors.EraseConfirmationMismatch);
        }

        _domainEvents.Add(new UserErased(Id));
        return ErasedUser.Create(Id, time.GetUtcNow(), actor.Id);
    }

    /// <summary>A-23: the role changed in Auth (<c>IdentityRoleChanged</c>). The same role again does nothing.</summary>
    public void SyncRole(ServerRole role, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (role == Role)
        {
            return;
        }

        Role = role;
        Record(time.GetUtcNow(), null, HistoryAction.RoleSynced, details: role.ToString());
    }

    /// <summary>UC-07: the user confirmed a new email in Auth (<c>IdentityEmailChanged</c>). The same email again does nothing.</summary>
    public void SyncEmail(Email email, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(time);
        if (email == Email)
        {
            return;
        }

        Email = email;
        Record(time.GetUtcNow(), null, HistoryAction.EmailSynced);
    }

    /// <summary>A-35: a sign-in or refresh in Auth. Events may arrive out of order, so an older time never moves it back.</summary>
    public void RecordSeen(DateTimeOffset at)
    {
        if (LastSeenAt is null || at > LastSeenAt)
        {
            LastSeenAt = at;
        }
    }
}
