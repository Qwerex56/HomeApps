using FluentResults;

namespace HomeMix.Domain.Boards;

/// <summary>
/// Where the widgets of a household stand in one place (its page or its wall tablet), on a grid of 8 columns with no
/// row limit (UI 2.4.1). Smaller screens (5 and 3 columns) flow from this one layout. Widgets that are not on the board
/// yet are placed by the web app after the ones that are; widgets that are gone are skipped.
/// </summary>
public sealed class WidgetBoard
{
    public const int Columns = 8;
    public const int MaxWidth = 8;
    public const int MaxHeight = 12;
    public const int MaxRow = 999;
    public const int MaxItems = 64;
    public const int MaxKeyLength = 128;

    private List<BoardItem> _items = [];

    private WidgetBoard() { }

    public Guid HouseholdId { get; private set; }

    public string Place { get; private set; } = string.Empty;

    public IReadOnlyList<BoardItem> Items => _items;

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid UpdatedBy { get; private set; }

    public static WidgetBoard Create(Guid householdId, string place)
    {
        if (!BoardPlaces.IsKnown(place))
        {
            throw new ArgumentException($"Unknown board place {place}.", nameof(place));
        }

        return new WidgetBoard { HouseholdId = householdId, Place = place };
    }

    /// <summary>Puts the whole layout in place of the old one, once it fits the grid and nothing overlaps.</summary>
    public Result Arrange(IReadOnlyList<BoardItem> items, Guid by, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count > MaxItems)
        {
            return Result.Fail(BoardErrors.TooMany);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (!seen.Add(item.Key))
            {
                return Result.Fail(BoardErrors.Duplicate(item.Key));
            }

            if (!BoardFit.IsKnown(item.Fit))
            {
                return Result.Fail(BoardErrors.Fit(item.Fit));
            }

            if (string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > MaxKeyLength
                || item.X < 0 || item.W is < 1 or > MaxWidth || item.X + item.W > Columns
                || item.Y < 0 || item.Y > MaxRow || item.H is < 1 or > MaxHeight)
            {
                return Result.Fail(BoardErrors.OutOfGrid(item.Key));
            }
        }

        for (var i = 0; i < items.Count; i++)
        {
            for (var j = i + 1; j < items.Count; j++)
            {
                if (Overlap(items[i], items[j]))
                {
                    return Result.Fail(BoardErrors.Overlap(items[i].Key, items[j].Key));
                }
            }
        }

        _items = [.. items];
        UpdatedBy = by;
        UpdatedAt = now;
        return Result.Ok();
    }

    private static bool Overlap(BoardItem a, BoardItem b) =>
        a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H;
}
