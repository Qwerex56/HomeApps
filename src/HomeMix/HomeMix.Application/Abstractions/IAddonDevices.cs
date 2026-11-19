using System.Text.Json;
using FluentResults;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Abstractions;

/// <summary>
/// Devices as a service for an add-on's trigger (D8): nobody is logged in, so HomeMix proves itself with its own client and
/// says whom it acts for. Devices decides again that the person is still an admin there. Anything unexpected throws.
/// </summary>
public interface IAddonDevices
{
    /// <summary>The household's devices with their last state, as the person (the installer) may see them; empty when they may see none.</summary>
    Task<IReadOnlyList<HouseholdDevice>> ListAsync(Guid householdId, Guid asUser, CancellationToken cancellationToken);

    /// <summary>Changes a control as the add-on (<c>POST /internal/devices/{id}/set</c>); fails with Devices' own error.</summary>
    Task<Result> SetAsync(Installation installation, string addonName, Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken);
}
