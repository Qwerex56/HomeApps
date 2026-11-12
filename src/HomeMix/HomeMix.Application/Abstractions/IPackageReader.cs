using FluentResults;

namespace HomeMix.Application.Abstractions;

/// <summary>Opens an add-on's zip without unpacking it to disk.</summary>
public interface IPackageReader
{
    Result<PackageContents> Read(byte[] zip);
}
