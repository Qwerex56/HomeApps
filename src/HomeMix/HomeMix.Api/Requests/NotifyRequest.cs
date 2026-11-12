namespace HomeMix.Api.Requests;

/// <summary><c>notify.push</c> from the bridge (docs/homemix.md, "Notifications").</summary>
public sealed record NotifyRequest(IReadOnlyList<Guid>? UserIds, string Title, string Body, string? Path, string? Tag);
