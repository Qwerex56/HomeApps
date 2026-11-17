using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Boards.SaveBoard;

/// <summary>The whole layout of a household's page or wall tablet, from the "Widżety" editor.</summary>
public sealed record SaveBoardCommand(Guid HouseholdId, string Place, IReadOnlyList<BoardItemInput> Items) : ICommand<BoardView>;
