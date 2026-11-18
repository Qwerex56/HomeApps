namespace Jarvis.Application.Abstractions;

/// <summary>Devices' <c>GET /internal/households/{id}/devices</c> with JARVIS's service secret, kept for a short while per household (H-3c).</summary>
public interface IHouseholdDevices
{
    /// <summary>The household's smart-home and assistant devices, or <c>null</c> when Devices cannot say (down, refused).</summary>
    Task<IReadOnlyList<HouseholdDevice>?> ListAsync(Guid householdId, CancellationToken cancellationToken);
}
