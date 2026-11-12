namespace HomeMix.Domain.Installations;

/// <summary>A household by its id, or a person's account by the user id.</summary>
public sealed record InstallTarget(TargetKind Kind, Guid Id)
{
    public static InstallTarget Household(Guid householdId) => new(TargetKind.Household, householdId);

    public static InstallTarget User(Guid userId) => new(TargetKind.User, userId);
}
