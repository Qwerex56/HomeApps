using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Boards.GetBoard;

/// <summary>Where a household's widgets stand on its page or its wall tablet, for any member.</summary>
public sealed record GetBoardQuery(Guid HouseholdId, string Place) : IQuery<BoardView>;
