using Auth.Domain.Common;
using Auth.Domain.Identities.Events;
using FluentResults;

namespace Auth.Domain.Identities;

/// <summary>Server role (A-23, owned by Auth) and the account status copied from Accounts (A-32).</summary>
public sealed partial class Identity
{
    /// <summary>
    /// A-23, R18: only the Owner grants or removes Admin; Owner is never given or taken through the API; nobody changes
    /// their own role. The same role again succeeds without an event. The new role reaches the JWT on the next refresh.
    /// </summary>
    public Result ChangeRole(Guid actorId, ServerRole actorRole, ServerRole newRole, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (actorRole != ServerRole.Owner || actorId == Id || newRole == ServerRole.Owner || Role == ServerRole.Owner)
        {
            return Result.Fail(IdentityErrors.ForbiddenRole);
        }

        if (newRole == Role)
        {
            return Result.Ok();
        }

        Role = newRole;
        Touch(time.GetUtcNow());
        _domainEvents.Add(new IdentityRoleChanged(Id, Role));
        return Result.Ok();
    }

    /// <summary>A-32: <c>UserDisabled</c> / <c>UserEnabled</c> from Accounts. Idempotent; any non-active status ends all sessions.</summary>
    public void ApplyStatus(AccountStatus status, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (Status == status)
        {
            return;
        }

        var now = time.GetUtcNow();
        Status = status;
        Touch(now);
        if (status != AccountStatus.Active)
        {
            RevokeAllSessions(now);
        }
    }
}
