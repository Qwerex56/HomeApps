namespace HomeMix.Domain.Boards;

/// <summary>The places of a household that have a board: its page and its wall tablet.</summary>
public static class BoardPlaces
{
    public const string Household = "Household";
    public const string Wall = "Wall";

    public static bool IsKnown(string place) => place is Household or Wall;
}
