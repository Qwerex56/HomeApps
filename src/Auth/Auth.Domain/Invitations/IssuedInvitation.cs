namespace Auth.Domain.Invitations;

/// <summary>A new invitation and the raw token for its link. The raw token goes into the email once and is never stored.</summary>
public sealed record IssuedInvitation(Invitation Invitation, string RawToken);
