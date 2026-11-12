using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Packages.AddOfficialPackage;

/// <summary>An official add-on that came with HomeMix, by its id; then installed like any other package.</summary>
public sealed record AddOfficialPackageCommand(string AddonId) : ICommand<PackageView>;
