namespace HomeMix.Api.Requests;

/// <param name="Reason">Why, for the Owner's own memory; shown only in the Owner's view.</param>
public sealed record BlockRequest(string? Reason);
