namespace Auth.Domain.Invitations;

public enum InvitationKind
{
    /// <summary>Someone with an active account invited this email (R10).</summary>
    Invite = 0,

    /// <summary>The first Owner, from <c>Auth:Bootstrap:Email</c> (A-30).</summary>
    Bootstrap = 1,
}

/// <summary>Derived from the stored dates, never stored itself.</summary>
public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Revoked = 2,
    Expired = 3,
}
