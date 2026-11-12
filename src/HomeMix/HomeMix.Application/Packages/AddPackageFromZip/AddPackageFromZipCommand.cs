using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Packages.AddPackageFromZip;

/// <summary>An uploaded zip, checked and kept, so the consent screen can show what it asks for.</summary>
public sealed record AddPackageFromZipCommand(byte[] Zip) : ICommand<PackageView>;
