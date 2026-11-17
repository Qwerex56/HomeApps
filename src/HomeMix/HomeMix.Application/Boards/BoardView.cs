using HomeMix.Domain.Boards;

namespace HomeMix.Application.Boards;

/// <summary>A household's board in one place; no items (and no date) while nobody has arranged it.</summary>
public sealed record BoardView(string Place, IReadOnlyList<BoardItem> Items, DateTimeOffset? UpdatedAt)
{
    public static BoardView From(WidgetBoard board) => new(board.Place, board.Items, board.UpdatedAt);

    public static BoardView Empty(string place) => new(place, [], null);
}
