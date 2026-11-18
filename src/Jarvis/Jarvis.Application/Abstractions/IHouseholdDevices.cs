namespace Jarvis.Application.Abstractions;

/// <summary>Devices' <c>GET /internal/households/{id}/devices</c> with JARVIS's service secret, kept for a short while per household and person (H-3c). Devices answers only for a member of the household or its own satellite.</summary>
public interface IHouseholdDevices
{
    /// <summary>The household's smart-home and assistant devices, or <c>null</c> when Devices cannot say (down, refused).</summary>
    /// <param name="fresh">Skip the cached answer and refresh it (a device paired a moment ago).</param>
    Task<IReadOnlyList<HouseholdDevice>?> ListAsync(Guid householdId, Guid userId, CancellationToken cancellationToken, bool fresh = false);
}
