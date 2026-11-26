using HomeMix.Application.Boards;

namespace HomeMix.Api.Requests;

/// <summary><c>PUT /homemix/api/households/{id}/boards/{place}</c>: every widget's key, column, row, width, height and fit. <c>ExpectedUpdatedAt</c> is the date the editor loaded; <c>Force</c> overwrites anyway.</summary>
public sealed record SaveBoardRequest(IReadOnlyList<BoardItemInput> Items, DateTimeOffset? ExpectedUpdatedAt = null, bool Force = false);
