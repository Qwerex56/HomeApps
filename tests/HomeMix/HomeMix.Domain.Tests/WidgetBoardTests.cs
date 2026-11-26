using HomeMix.Domain.Boards;

namespace HomeMix.Domain.Tests;

public sealed class WidgetBoardTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

    private static BoardItem Item(string key, int x, int y, int w = 1, int h = 1, string fit = BoardFit.Center) => new(key, x, y, w, h, fit);

    [Fact]
    public void A_layout_on_the_grid_replaces_the_old_one()
    {
        var board = WidgetBoard.Create(Guid.NewGuid(), BoardPlaces.Household);
        var by = Guid.NewGuid();
        board.Arrange([Item("core:household", 0, 0, 3, 2)], by, Now);

        var arranged = board.Arrange([Item("core:household", 5, 0, 3, 2), Item("mix:a", 0, 0, 2, 1, BoardFit.Fill), Item("mix:b", 0, 40)], by, Now);

        Assert.True(arranged.IsSuccess);
        Assert.Equal(3, board.Items.Count);
        Assert.Equal(by, board.UpdatedBy);
        Assert.Equal(Now, board.UpdatedAt);
    }

    [Theory]
    [InlineData(6, 0, 3, 1, "homemix.board_out_of_grid")]
    [InlineData(0, 0, 9, 1, "homemix.board_out_of_grid")]
    [InlineData(0, 0, 1, 0, "homemix.board_out_of_grid")]
    [InlineData(-1, 0, 1, 1, "homemix.board_out_of_grid")]
    [InlineData(1, 0, 2, 2, "homemix.board_overlap")]
    public void A_widget_off_the_grid_or_over_another_is_refused(int x, int y, int w, int h, string code)
    {
        var board = WidgetBoard.Create(Guid.NewGuid(), BoardPlaces.Wall);

        var arranged = board.Arrange([Item("core:household", 0, 0, 2, 1), Item("mix:a", x, y, w, h)], Guid.NewGuid(), Now);

        Assert.Equal(code, Assert.IsType<HomeApps.BuildingBlocks.Domain.Errors.AppError>(arranged.Errors[0]).Code);
        Assert.Empty(board.Items);
    }

    [Fact]
    public void A_widget_can_be_as_wide_as_the_whole_grid()
    {
        var board = WidgetBoard.Create(Guid.NewGuid(), BoardPlaces.Wall);

        var arranged = board.Arrange([Item("mix:a", 0, 0, 8, 6)], Guid.NewGuid(), Now);

        Assert.True(arranged.IsSuccess);
    }

    [Fact]
    public void Twice_the_same_widget_or_an_unknown_fit_is_refused()
    {
        var board = WidgetBoard.Create(Guid.NewGuid(), BoardPlaces.Household);

        var twice = board.Arrange([Item("mix:a", 0, 0), Item("mix:a", 1, 0)], Guid.NewGuid(), Now);
        var fit = board.Arrange([Item("mix:a", 0, 0, fit: "Stretch")], Guid.NewGuid(), Now);

        Assert.True(twice.IsFailed);
        Assert.True(fit.IsFailed);
    }
}
