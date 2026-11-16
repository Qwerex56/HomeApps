namespace HomeMix.Domain.Installations;

/// <summary>Where an installation's widgets can stand: a household's page and its wall tablet, or "Moje aplikacje".</summary>
public static class WidgetPlaces
{
    public const string Household = "Household";
    public const string Wall = "Wall";
    public const string Account = "Account";

    public static IReadOnlyList<string> For(TargetKind kind) => kind == TargetKind.Household ? [Household, Wall] : [Account];
}
