using Households.Domain.Common;
using FluentResults;

namespace Households.Domain.Households;

/// <summary>The household's assistant (D1, D2, D6, D9): everyone with access looks, Admin and up change.</summary>
public sealed partial class Household
{
    public HouseholdAssistant Assistant { get; private set; } = HouseholdAssistant.CreateDefault();

    /// <summary>Anyone with access, guests too (D6), and server admins (D8); a stranger gets not found (Q3).</summary>
    public Result<HouseholdAssistant> ViewAssistant(Actor actor, DateTimeOffset now) =>
        CanRead(actor, now) ? Assistant : Result.Fail<HouseholdAssistant>(HouseholdErrors.NotFound);

    /// <summary>D6: Owner and Admin only. Changing nothing is fine.</summary>
    public Result UpdateAssistant(
        Actor actor, bool enabled, string? name, AssistantAccessLevel accessLevel, AssistantToolSources toolSources,
        int eventDays, int locationEventDays, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Admin);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        var updated = Assistant.Update(enabled, name, accessLevel, toolSources, eventDays, locationEventDays);
        if (updated.IsSuccess)
        {
            Touch(now);
        }

        return updated;
    }

    /// <summary>
    /// The role a satellite of this household gets (S13): from the assistant's access level. An archived household or a
    /// disabled assistant is the same as no access (404).
    /// </summary>
    public Result<HouseholdRole> AssistantRole() =>
        Status == HouseholdStatus.Active && Assistant.Enabled ? Assistant.Role : Result.Fail<HouseholdRole>(HouseholdErrors.NotFound);
}
