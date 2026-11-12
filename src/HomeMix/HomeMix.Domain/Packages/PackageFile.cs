namespace HomeMix.Domain.Packages;

/// <summary>A file in an add-on's zip: its path inside the zip and its unpacked size.</summary>
public sealed record PackageFile(string Path, long Length);
