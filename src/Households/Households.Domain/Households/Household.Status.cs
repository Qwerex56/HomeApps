using Households.Domain.Common;
using Households.Domain.Households.Events;
using FluentResults;

namespace Households.Domain.Households;

/// <summary>Name and description (H4), archiving and restoring (H5, H6, D7, D8).</summary>
public sealed partial class Household
{
    /// <summary>H4: the Owner or an Admin; no change succeeds without an event.</summary>
    public Result Update(Actor actor, string? name, string? description, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Admin);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        var validName = HouseholdName.Create(name);
        var validDescription = HouseholdDescription.Create(description);
        var check = Result.Merge(validName, validDescription);
        if (check.IsFailed)
        {
            return check;
        }

        if (validName.Value == Name && validDescription.Value == Description)
        {
            return Result.Ok();
        }

        Name = validName.Value;
        Description = validDescription.Value;
        Touch(now);
        _domainEvents.Add(new HouseholdUpdated(Id, Name.Value, Description?.Value));
        return Result.Ok();
    }

    /// <summary>
    /// H5, D7 ("delete household"): the Owner or a server admin. The household disappears for its members and its invites
    /// stop working; the data stays for audit.
    /// </summary>
    public Result Archive(Actor actor, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        if (!actor.IsServerAdmin)
        {
            var acting = Acting(actor, now, HouseholdRole.Owner);
            if (acting.IsFailed)
            {
                return acting.ToResult();
            }
        }
        else if (Status == HouseholdStatus.Archived)
        {
            return Result.Fail(HouseholdErrors.AlreadyArchived);
        }

        ArchiveNow(now);
        return Result.Ok();
    }

    /// <summary>H6, D7: only a server admin restores; to anybody else an archived household does not exist.</summary>
    public Result Restore(Actor actor, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        if (!actor.IsServerAdmin)
        {
            return Result.Fail(HouseholdErrors.NotFound);
        }

        if (Status != HouseholdStatus.Archived)
        {
            return Result.Fail(HouseholdErrors.NotArchived);
        }

        if (Owner is null)
        {
            return Result.Fail(HouseholdErrors.NoOwner);
        }

        Status = HouseholdStatus.Active;
        ArchivedAt = null;
        Touch(time.GetUtcNow());
        _domainEvents.Add(new HouseholdRestored(Id));
        return Result.Ok();
    }

    private void ArchiveNow(DateTimeOffset now)
    {
        Status = HouseholdStatus.Archived;
        ArchivedAt = now;
        Touch(now);
        _domainEvents.Add(new HouseholdArchived(Id));
    }
}
