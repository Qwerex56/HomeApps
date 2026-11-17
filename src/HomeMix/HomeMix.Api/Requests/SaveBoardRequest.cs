using HomeMix.Application.Boards;

namespace HomeMix.Api.Requests;

/// <summary><c>PUT /homemix/api/households/{id}/boards/{place}</c>: every widget's key, column, row, width, height and fit.</summary>
public sealed record SaveBoardRequest(IReadOnlyList<BoardItemInput> Items);
