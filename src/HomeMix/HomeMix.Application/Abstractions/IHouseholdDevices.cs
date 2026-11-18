using System.Text.Json;
using FluentResults;

namespace HomeMix.Application.Abstractions;

/// <summary>Asked from Devices with the caller's own token, so Devices applies its own rules (guests read, members control).</summary>
public interface IHouseholdDevices
{
    /// <summary>
    /// The household's devices that are not removed, narrowed by the filters Devices knows (capability, role, model);
    /// with <paramref name="withState"/> each carries its last state. Empty without access.
    /// </summary>
    Task<IReadOnlyList<HouseholdDevice>> ListAsync(
        Guid householdId, string? capability, string? role, string? model, bool withState, CancellationToken cancellationToken);

    /// <summary>The device's state as Devices answers it, or <c>null</c> when the caller may not see it.</summary>
    Task<JsonElement?> GetStateAsync(Guid deviceId, CancellationToken cancellationToken);

    /// <summary>Changes a control; fails with Devices' own error (<c>devices.offline</c>, <c>devices.read_only</c>, ...).</summary>
    Task<Result> SetAsync(Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken);
}
