using HomeMix.Domain.Installations;

namespace HomeMix.Application.Realtime;

/// <summary>
/// The hub's groups: every connection is in its person's <c>user:{id}</c>, and in <c>household:{id}</c> for each
/// household it watched as a member. Events only name what changed (keys, versions), never data.
/// </summary>
public static class RealtimeGroups
{
    public static string User(Guid userId) => $"user:{userId}";

    public static string Household(Guid householdId) => $"household:{householdId}";

    /// <summary>Who uses an installation: its household, or the person it is installed for.</summary>
    public static string Of(InstallTarget target) =>
        target.Kind == TargetKind.Household ? Household(target.Id) : User(target.Id);
}
