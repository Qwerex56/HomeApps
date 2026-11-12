using System.Collections.Concurrent;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Application.Gateway;

namespace Jarvis.Api.Tests;

/// <summary>Auth's delegation without Auth: a delegated test token, and who asked for it.</summary>
internal sealed class FakeDelegation : IDelegation
{
    public ConcurrentQueue<(Guid UserId, string Client)> Calls { get; } = new();

    public Task<string> TokenForAsync(Guid userId, string client, CancellationToken cancellationToken)
    {
        Calls.Enqueue((userId, client));
        return Task.FromResult(TestJwt.CreateDelegatedToken(userId));
    }
}
