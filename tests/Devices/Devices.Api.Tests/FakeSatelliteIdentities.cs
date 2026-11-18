using Devices.Application.Abstractions;
using FluentResults;

namespace Devices.Api.Tests;

/// <summary>Auth's pairing endpoint: remembers who was paired and answers with a secret naming the device.</summary>
public sealed class FakeSatelliteIdentities : ISatelliteIdentities
{
    public List<(Guid DeviceId, Guid HouseholdId, string Name)> Paired { get; } = [];

    public Task<Result<string>> PairAsync(Guid deviceId, Guid householdId, string name, CancellationToken cancellationToken)
    {
        lock (Paired)
        {
            Paired.Add((deviceId, householdId, name));
        }

        return Task.FromResult(Result.Ok($"secret-of-{deviceId}"));
    }
}
