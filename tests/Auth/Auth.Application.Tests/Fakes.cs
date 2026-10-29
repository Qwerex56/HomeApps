using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts;

namespace Auth.Application.Tests;

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed class FakeHasher : IPasswordHasher
{
    public int Verifications { get; private set; }

    public string Hash(string password) => "hashed:" + password;

    public bool Verify(string hash, string password)
    {
        Verifications++;
        return hash == Hash(password);
    }
}

internal sealed class InMemoryIdentities : IIdentityRepository
{
    public List<Identity> Items { get; } = [];

    public Task<Identity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(i => i.Id == id));

    public Task<Identity?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(i => i.Email == email));

    public Task<Identity?> GetByExternalLoginAsync(ExternalProvider provider, string subject, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(i => i.ExternalLogins.Any(l => l.Provider == provider && l.Subject == subject)));

    public Task<Identity?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(i => i.Sessions.Any(s => s.RefreshTokens.Any(t => t.TokenHash == tokenHash))));

    public Task<Identity?> GetByEmailTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(i => i.EmailToken?.TokenHash == tokenHash));

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Exists(i => i.Email == email));

    public Task<bool> ExternalLoginExistsAsync(ExternalProvider provider, string subject, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Exists(i => i.ExternalLogins.Any(l => l.Provider == provider && l.Subject == subject)));

    public Task<bool> OwnerExistsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Items.Exists(i => i.Role == ServerRole.Owner));

    public void Add(Identity identity) => Items.Add(identity);

    public void Remove(Identity identity) => Items.Remove(identity);
}

internal sealed class InMemoryInvitations : IInvitationRepository
{
    public List<Invitation> Items { get; } = [];

    public Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(i => i.Id == id));

    public Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(i => i.TokenHash == tokenHash));

    public Task<bool> HasPendingForAsync(Email email, DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Exists(i => i.Email == email && i.StatusAt(now) == InvitationStatus.Pending));

    public Task<bool> HasPendingBootstrapAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Exists(i => i.Kind == InvitationKind.Bootstrap && i.StatusAt(now) == InvitationStatus.Pending));

    public Task<IReadOnlyList<Invitation>> ListAsync(Guid? invitedBy, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Invitation>>(
            [.. Items.Where(i => invitedBy is null || i.InvitedByUserId == invitedBy).OrderByDescending(i => i.CreatedAt)]);

    public Task<Invitation?> GetAcceptedByAsync(Guid identityId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(i => i.AcceptedByIdentityId == identityId));

    public void Add(Invitation invitation) => Items.Add(invitation);
}

internal sealed class FakeClients : IClientRegistry
{
    public static readonly RegisteredClient Web = new("homeapps-web", "HomeApps", ClientType.Browser, [], ["homeapps"]);

    public static readonly RegisteredClient Jarvis = new("jarvis", "JARVIS", ClientType.Native, [], ["homeapps"]);

    public RegisteredClient? Find(string clientId) => new[] { Web, Jarvis }.FirstOrDefault(c => c.ClientId == clientId);
}

/// <summary>Access tokens name the identity and session; tickets are handles to the records kept here.</summary>
internal sealed class FakeTokens : ITokenIssuer
{
    private readonly Dictionary<string, Ticket> _tickets = [];

    public AccessToken IssueAccessToken(Identity identity, IssuedRefreshToken session, RegisteredClient client) =>
        new($"access:{identity.Id}:{identity.Role}:{session.SessionId}", session.AuthenticatedAt.AddMinutes(15));

    public string IssueTicket(Ticket ticket)
    {
        var value = $"ticket-{_tickets.Count + 1}";
        _tickets[value] = ticket;
        return value;
    }

    public Task<Ticket?> ReadTicketAsync(string value) => Task.FromResult(_tickets.GetValueOrDefault(value));
}

internal sealed class FakeVerifier : IExternalIdentityVerifier
{
    public Result<ExternalIdentity> Next { get; set; } =
        new ExternalIdentity("google-sub-1", "ewa@example.com", true, "Ewa", "Kowalska", null);

    public Task<Result<ExternalIdentity>> VerifyAsync(
        ExternalProvider provider, string code, string codeVerifier, Uri redirectUri, CancellationToken cancellationToken) =>
        Task.FromResult(Next);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    /// <summary>Simulates another request saving the same rows first.</summary>
    public bool LoseNextRace { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (LoseNextRace)
        {
            LoseNextRace = false;
            throw new ConcurrencyConflictException();
        }

        Saves++;
        return Task.FromResult(0);
    }
}

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }

    public ServerRole Role { get; set; } = ServerRole.User;

    public DateTimeOffset? AuthenticatedAt { get; set; }

    public bool IsAuthenticated => UserId is not null;

    public string? Email => null;

    public IReadOnlyCollection<string> Roles => [Role.ToString()];

    public bool IsInRole(string role) => Roles.Contains(role);

    public Guid RequiredUserId => UserId ?? throw new InvalidOperationException("Not signed in.");
}

internal sealed class FakeEventBus : IEventBus
{
    public List<IIntegrationEvent> Published { get; } = [];

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IIntegrationEvent
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}
