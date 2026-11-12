using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.GetHouseholdInstallations;

/// <summary>The add-ons installed in a household, for any member.</summary>
public sealed record GetHouseholdInstallationsQuery(Guid HouseholdId) : IQuery<IReadOnlyList<InstallationView>>;
