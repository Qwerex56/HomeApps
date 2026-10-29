using Auth.Domain.Common;

namespace Auth.Domain.Invitations;

/// <summary>What the handler knows about the person sending an invitation.</summary>
public sealed record Inviter(Guid UserId, Email Email, bool CanSignIn);
