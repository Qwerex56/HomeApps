using HomeApps.Contracts.Households;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;

namespace HomeMix.Api.Mcp;

/// <summary>One installation of an add-on that the caller reaches and agreed to JARVIS for, with how they name it.</summary>
/// <param name="Name">The household's name, or <see cref="ToolCatalog.AccountName"/> for the caller's own account.</param>
/// <param name="Role">The caller's household role; Owner on their own account.</param>
public sealed record ToolPlace(Installation Installation, AddonManifest Manifest, string Name, HouseholdRole Role);
