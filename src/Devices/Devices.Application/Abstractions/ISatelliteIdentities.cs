using FluentResults;

namespace Devices.Application.Abstractions;

/// <summary>
/// Auth's side of pairing (jarvis-satellites.md 3a, S13): the satellite's own identity, made or renewed on Auth's
/// internal endpoint with Devices' service secret. Returns the secret Auth hands out once.
/// </summary>
public interface ISatelliteIdentities
{
    Task<Result<string>> PairAsync(Guid deviceId, Guid householdId, string name, CancellationToken cancellationToken);
}
