namespace HomeMix.Domain.Boards;

/// <summary>How a widget sits in a wall tablet cell that is not its standard size.</summary>
public static class BoardFit
{
    /// <summary>Centred in the biggest standard size that fits (the default).</summary>
    public const string Center = "Center";

    /// <summary>Stretched over the whole cell ("explode").</summary>
    public const string Fill = "Fill";

    public static bool IsKnown(string fit) => fit is Center or Fill;
}
