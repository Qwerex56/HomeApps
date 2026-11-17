namespace HomeMix.Api.Requests;

/// <summary><c>PUT /homemix/api/me/pins</c>: my account's add-ons on the quick links bar, in order (at most 8).</summary>
public sealed record PinMyAppsRequest(IReadOnlyList<Guid> Installations);
