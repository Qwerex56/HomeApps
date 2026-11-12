namespace HomeMix.Domain.Manifests;

/// <summary>A named permission and the lowest household role that has it (<c>Guest</c>, <c>Member</c>, <c>Admin</c>, <c>Owner</c>).</summary>
public sealed record AddonPermission(string Id, string MinRole);
