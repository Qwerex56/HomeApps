namespace HomeMix.Application.Abstractions;

/// <summary>The official add-ons that came with this HomeMix (<c>HomeMix:OfficialPath</c>).</summary>
public interface IOfficialAddons
{
    IReadOnlyList<OfficialAddon> List();

    /// <summary>The zip of an official add-on, or <c>null</c> when this HomeMix has none with that id.</summary>
    Task<byte[]?> ReadAsync(string addonId, CancellationToken cancellationToken);
}
