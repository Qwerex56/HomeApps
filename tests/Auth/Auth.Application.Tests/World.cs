using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Application.Register;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using Microsoft.Extensions.Options;

namespace Auth.Application.Tests;

/// <summary>Everything a handler needs, in memory. Handlers are called directly; the pipeline has its own test.</summary>
internal sealed class World
{
    public const string Password = "correct horse battery";

    public FixedTime Time { get; } = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    public FakeHasher Hasher { get; } = new();

    public InMemoryIdentities Identities { get; } = new();

    public InMemoryInvitations Invitations { get; } = new();

    public FakeClients Clients { get; } = new();

    public FakeTokens Tokens { get; } = new();

    public FakeVerifier Verifier { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public FakeCurrentUser CurrentUser { get; } = new();

    public FakeEventBus Bus { get; } = new();

    public AuthOptions Options { get; } = new() { RegistrationOpen = true };

    public IOptions<AuthOptions> OptionsAccessor => Microsoft.Extensions.Options.Options.Create(Options);

    /// <summary>A registered, active password identity with one session; domain events cleared.</summary>
    public async Task<(Identity Identity, AuthTokens Tokens)> Registered(string email = "adam@example.com")
    {
        var handler = new RegisterWithPasswordHandler(Identities, Clients, Tokens, Hasher, OptionsAccessor, Time);
        var tokens = await handler.Handle(
            new RegisterWithPasswordCommand(email, Password, "Adam", null, null, "pl-PL", FakeClients.Web.ClientId, "Firefox"),
            CancellationToken.None);
        var identity = Identities.Items.Single(i => i.Email.Value == email);
        identity.ClearDomainEvents();
        return (identity, tokens.Value);
    }

    /// <summary>Signs <paramref name="identity"/> in as the current user, authenticated just now.</summary>
    public void SignInAs(Identity identity)
    {
        CurrentUser.UserId = identity.Id;
        CurrentUser.Role = identity.Role;
        CurrentUser.AuthenticatedAt = Time.Now;
    }
}

internal static class ResultExtensions
{
    public static string Code(this ResultBase result) => (string)result.Errors[0].Metadata[AppError.CodeKey];
}
