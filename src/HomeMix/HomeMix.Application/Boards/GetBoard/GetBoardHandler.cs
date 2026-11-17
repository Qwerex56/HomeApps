using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Boards;

namespace HomeMix.Application.Boards.GetBoard;

internal sealed class GetBoardHandler(IBoardRepository boards, IHouseholdRoles households) : IQueryHandler<GetBoardQuery, BoardView>
{
    public async Task<Result<BoardView>> Handle(GetBoardQuery query, CancellationToken cancellationToken)
    {
        if (!BoardPlaces.IsKnown(query.Place) || await households.GetRoleAsync(query.HouseholdId, cancellationToken) is null)
        {
            return Result.Fail(BoardErrors.NotFound);
        }

        var board = await boards.FindAsync(query.HouseholdId, query.Place, cancellationToken);
        return board is null ? BoardView.Empty(query.Place) : BoardView.From(board);
    }
}
