using Accounts.Domain.Common;
using Accounts.Domain.Users.Events;
using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>Who may change the account (R1) and the profile (UC-02, UC-13).</summary>
public sealed partial class User
{
    /// <summary>
    /// R1: everyone manages their own account; someone else's only with a higher role (<c>Admin</c> manages <c>User</c>,
    /// <c>Owner</c> manages <c>User</c> and <c>Admin</c>). The Owner's account is changed by nobody but the Owner.
    /// </summary>
    public bool CanBeManagedBy(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return actor.Id == Id || (Role != ServerRole.Owner && actor.Role > Role);
    }

    /// <summary>
    /// UC-02, UC-13: replaces the profile. R1; a disabled account is edited only by a higher role (the user restores it
    /// through <see cref="ReactivateSelf"/>). Nothing changed: success, no history, no event.
    /// </summary>
    public Result UpdateProfile(Actor actor, Profile profile, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(time);

        if (!CanBeManagedBy(actor))
        {
            return Result.Fail(UserErrors.ForbiddenRole);
        }

        if (Status == AccountStatus.Disabled && actor.Id == Id)
        {
            return Result.Fail(UserErrors.AccountDisabled);
        }

        ApplyProfile(actor.Id, profile, time.GetUtcNow());
        return Result.Ok();
    }

    private void ApplyProfile(Guid actorId, Profile profile, DateTimeOffset now)
    {
        var changed = profile.ChangedFrom(Profile);
        if (changed.Count == 0)
        {
            return;
        }

        var visibleToOthers = profile.DisplayName != Profile.DisplayName || profile.AvatarUrl != Profile.AvatarUrl;
        Profile = profile;
        Record(now, actorId, HistoryAction.ProfileUpdated, details: string.Join(", ", changed));
        if (visibleToOthers)
        {
            _domainEvents.Add(new UserProfileUpdated(Id, profile.DisplayName.Value, profile.AvatarUrl?.Value));
        }
    }
}
