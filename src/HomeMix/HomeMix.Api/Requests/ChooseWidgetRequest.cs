namespace HomeMix.Api.Requests;

/// <summary><c>PUT /homemix/api/installations/{id}/widgets/{place}</c>: a widget id (or null for none), a size and an order.</summary>
public sealed record ChooseWidgetRequest(string? Widget, string? Size, int Order = 0);
