using System.Text.Json;
using FluentResults;

namespace Devices.Application.Abstractions;

/// <summary>
/// The devices connected to <c>/devices/link</c> right now (jarvis-satellites.md 2a). Kept in memory by the one
/// Devices instance; a device that is not connected is offline.
/// </summary>
public interface IDeviceLinks
{
    bool IsOnline(Guid deviceId);

    /// <summary>Sends the action to the device and waits for its answer (plain text), failing when it is offline or silent.</summary>
    Task<Result<string>> RunAsync(Guid deviceId, string action, JsonElement? input, CancellationToken cancellationToken);

    /// <summary>Sends <c>set</c> to the device and waits for its <c>set_result</c>, failing when it is offline or silent.</summary>
    Task<Result> SetAsync(Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken);

    /// <summary>Closes the device's connection, e.g. after it was removed.</summary>
    void Drop(Guid deviceId);
}
