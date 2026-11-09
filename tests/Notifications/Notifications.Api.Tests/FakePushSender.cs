using System.Collections.Concurrent;
using Notifications.Application.Abstractions;
using Notifications.Domain.Recipients;

namespace Notifications.Api.Tests;

/// <summary>Records what would go to push services; endpoints in <see cref="Gone"/> answer 410.</summary>
public sealed class FakePushSender : IPushSender
{
    public ConcurrentQueue<(PushTarget Target, PushMessage Message)> Sent { get; } = new();

    public ConcurrentDictionary<string, bool> Gone { get; } = new();

    public Task<PushOutcome> SendAsync(PushTarget target, PushMessage message, CancellationToken cancellationToken)
    {
        if (Gone.ContainsKey(target.Endpoint))
        {
            return Task.FromResult(PushOutcome.Gone);
        }

        Sent.Enqueue((target, message));
        return Task.FromResult(PushOutcome.Delivered);
    }

    public IReadOnlyList<PushMessage> SentTo(string endpoint) =>
        Sent.Where(s => s.Target.Endpoint == endpoint).Select(s => s.Message).ToList();
}
