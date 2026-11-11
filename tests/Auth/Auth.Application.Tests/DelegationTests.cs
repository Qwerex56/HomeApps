using Auth.Application.Delegation.DelegateToAssistant;
using Auth.Domain.Identities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Auth.Application.Tests;

/// <summary>jarvis-v1.md, JV3: a service client gets a short token to act for one active person.</summary>
public sealed class DelegationTests
{
    private readonly World _w = new();

    private DelegateToAssistantHandler Handler() =>
        new(_w.Clients, _w.Identities, _w.Tokens, NullLogger<DelegateToAssistantHandler>.Instance);

    private static DelegateToAssistantCommand For(Guid userId, string clientId = "jarvis-service", string secret = FakeClients.JarvisServiceSecret) =>
        new(clientId, secret, userId, "chat", "conversation/message");

    [Fact]
    public async Task The_service_gets_a_token_for_an_active_person()
    {
        var (identity, _) = await _w.Registered();

        var result = await Handler().Handle(For(identity.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal($"delegated:{identity.Id}:jarvis-service:jarvis-service", result.Value.AccessToken);
    }

    [Theory]
    [InlineData("jarvis-service", "wrong-secret")]
    [InlineData("homeapps-web", FakeClients.JarvisServiceSecret)]
    [InlineData("nobody", FakeClients.JarvisServiceSecret)]
    public async Task Only_a_service_with_its_secret_may_ask(string clientId, string secret)
    {
        var (identity, _) = await _w.Registered();

        var result = await Handler().Handle(For(identity.Id, clientId, secret), default);

        Assert.Equal("delegation.client_invalid", result.Code());
    }

    [Fact]
    public async Task Not_for_a_disabled_or_unknown_person()
    {
        var (identity, _) = await _w.Registered();
        identity.ApplyStatus(AccountStatus.DisabledByAdmin, _w.Time);

        var disabled = await Handler().Handle(For(identity.Id), default);
        var unknown = await Handler().Handle(For(Guid.NewGuid()), default);

        Assert.Equal("delegation.user_unavailable", disabled.Code());
        Assert.Equal("delegation.user_unavailable", unknown.Code());
    }

    [Fact]
    public async Task A_service_client_cannot_sign_anyone_in()
    {
        var handler = new Login.SignInWithPasswordHandler(_w.Identities, _w.Clients, _w.Tokens, _w.Hasher, _w.UnitOfWork, _w.Time);
        await _w.Registered();

        var result = await handler.Handle(
            new Login.SignInWithPasswordCommand("adam@example.com", World.Password, "jarvis-service", "cli"), default);

        Assert.Equal("client.unknown", result.Code());
    }
}
