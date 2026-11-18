using System.Collections.Concurrent;
using Jarvis.Application.Voice;

namespace Jarvis.Api.Tests;

/// <summary>Households and Auth without them: whose PIN is whose, and what JARVIS asked.</summary>
internal sealed class FakeSpeakers : ISpeakers
{
    public ConcurrentDictionary<string, (Guid UserId, string Name)> Pins { get; } = new();

    public ConcurrentQueue<(Guid DeviceId, Guid HouseholdId, string? Name)> Asked { get; } = new();

    public Task<SpeakerMatch> IdentifyAsync(Guid deviceId, Guid householdId, string? name, string pin, CancellationToken cancellationToken)
    {
        Asked.Enqueue((deviceId, householdId, name));
        return Task.FromResult(Pins.TryGetValue(pin, out var person)
            ? new SpeakerMatch(SpeakerOutcome.Known, person.UserId, person.Name)
            : new SpeakerMatch(SpeakerOutcome.Unknown));
    }
}
