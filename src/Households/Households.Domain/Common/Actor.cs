namespace Households.Domain.Common;

/// <summary>Who performs an action: the caller's id and server role, always from the access token, never from the body.</summary>
public sealed record Actor(Guid Id, ServerRole Role)
{
    /// <summary>D8: server <c>Admin</c> and <c>Owner</c> see every household and may archive and restore it.</summary>
    public bool IsServerAdmin => Role >= ServerRole.Admin;
}
