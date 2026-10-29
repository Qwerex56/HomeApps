namespace Accounts.Domain.Common;

/// <summary>Who performs an action: the caller's id and role, always from the access token, never from the body.</summary>
public sealed record Actor(Guid Id, ServerRole Role);
