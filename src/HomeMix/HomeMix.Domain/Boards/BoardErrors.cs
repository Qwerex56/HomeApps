using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Domain.Boards;

public static class BoardErrors
{
    public static Error NotFound => new AppError("homemix.board_not_found", "This household has no such board.", ErrorKind.NotFound);

    public static Error Forbidden => new AppError(
        "homemix.board_forbidden", "Only a household's admins can arrange its widgets.", ErrorKind.Forbidden);

    public static Error Conflict => new AppError(
        "homemix.board_conflict", "The board was changed by someone else meanwhile.", ErrorKind.Conflict);

    public static Error TooMany => new AppError(
        "homemix.board_too_many", $"A board holds at most {WidgetBoard.MaxItems} widgets.", ErrorKind.Validation);

    public static Error Duplicate(string key) => new AppError(
        "homemix.board_duplicate", $"The widget {key} stands on the board twice.", ErrorKind.Validation);

    public static Error OutOfGrid(string key) => new AppError(
        "homemix.board_out_of_grid",
        $"The widget {key} does not fit the grid: {WidgetBoard.Columns} columns, width 1 to {WidgetBoard.MaxWidth}, height 1 to {WidgetBoard.MaxHeight}.",
        ErrorKind.Validation);

    public static Error Overlap(string first, string second) => new AppError(
        "homemix.board_overlap", $"The widgets {first} and {second} overlap.", ErrorKind.Validation);

    public static Error Fit(string fit) => new AppError(
        "homemix.board_fit", $"Fit is {BoardFit.Center} or {BoardFit.Fill}, not {fit}.", ErrorKind.Validation);
}
