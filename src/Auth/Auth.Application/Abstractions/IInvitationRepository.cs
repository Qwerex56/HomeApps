using Auth.Domain.Common;
using Auth.Domain.Invitations;

namespace Auth.Application.Abstractions;

public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>R14: a not accepted, not revoked invitation to this email that has not expired at <paramref name="now"/>.</summary>
    Task<bool> HasPendingForAsync(Email email, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>A-30: a pending bootstrap invitation exists, so a restart creates no new one.</summary>
    Task<bool> HasPendingBootstrapAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Newest first. <paramref name="invitedBy"/> null lists everyone's invitations.</summary>
    Task<IReadOnlyList<Invitation>> ListAsync(Guid? invitedBy, CancellationToken cancellationToken);

    /// <summary>The invitation through which this identity signed up, if any (P7).</summary>
    Task<Invitation?> GetAcceptedByAsync(Guid identityId, CancellationToken cancellationToken);

    void Add(Invitation invitation);
}
