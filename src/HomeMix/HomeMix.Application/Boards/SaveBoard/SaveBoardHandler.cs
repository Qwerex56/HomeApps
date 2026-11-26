using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Realtime;
using HomeMix.Domain.Boards;

namespace HomeMix.Application.Boards.SaveBoard;

internal sealed class SaveBoardHandler(
    IBoardRepository boards,
    IHouseholdRoles households,
    ICurrentUser currentUser,
    TimeProvider time,
    RealtimeOutbox realtime) : ICommandHandler<SaveBoardCommand, BoardView>
{
    public async Task<Result<BoardView>> Handle(SaveBoardCommand command, CancellationToken cancellationToken)
    {
        var role = await households.GetRoleAsync(command.HouseholdId, cancellationToken);
        if (!BoardPlaces.IsKnown(command.Place) || role is null)
        {
            return Result.Fail(BoardErrors.NotFound);
        }

        if (role < HouseholdRole.Admin)
        {
            return Result.Fail(BoardErrors.Forbidden);
        }

        var board = await boards.FindAsync(command.HouseholdId, command.Place, cancellationToken);
        if (board is not null && !command.Force
            && (command.ExpectedUpdatedAt is null || Math.Abs((board.UpdatedAt - command.ExpectedUpdatedAt.Value).TotalMilliseconds) > 1))
        {
            return Result.Fail(BoardErrors.Conflict);
        }

        if (board is null)
        {
            board = WidgetBoard.Create(command.HouseholdId, command.Place);
            boards.Add(board);
        }

        var items = command.Items.Select(i => new BoardItem(i.Key, i.X, i.Y, i.W, i.H, i.Fit ?? BoardFit.Center)).ToList();
        var arranged = board.Arrange(items, currentUser.RequiredUserId, time.GetUtcNow());
        if (arranged.IsFailed)
        {
            return arranged;
        }

        realtime.Add(RealtimeEvents.BoardChanged(command.HouseholdId, command.Place));
        return BoardView.From(board);
    }
}
