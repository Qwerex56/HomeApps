using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Boards.SaveBoard;

/// <summary>The whole layout of a household's page or wall tablet, from the "Widżety" editor.
/// An already arranged board is only replaced when <c>ExpectedUpdatedAt</c> is the date the editor loaded, or when <c>Force</c> is set.</summary>
public sealed record SaveBoardCommand(Guid HouseholdId, string Place, IReadOnlyList<BoardItemInput> Items, DateTimeOffset? ExpectedUpdatedAt = null, bool Force = false) : ICommand<BoardView>;
