using System.IO.Compression;
using System.Text;
using FluentResults;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Packages;

namespace HomeMix.Infrastructure.Storage;

/// <summary>Lists a zip's files and reads homemix.json from its root, in memory, refusing obvious zip bombs early.</summary>
internal sealed class ZipPackageReader : IPackageReader
{
    private const int MaxManifestBytes = 64 * 1024;

    public Result<PackageContents> Read(byte[] zip)
    {
        ArgumentNullException.ThrowIfNull(zip);
        try
        {
            using var archive = new ZipArchive(new MemoryStream(zip, writable: false), ZipArchiveMode.Read);
            var files = new List<PackageFile>();
            string? manifest = null;
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith('/'))
                {
                    continue;
                }

                files.Add(new PackageFile(entry.FullName, entry.Length));
                if (files.Count > PackageRules.MaxFiles || files.Sum(f => f.Length) > PackageRules.MaxUnpackedBytes)
                {
                    break;
                }

                if (entry.FullName == PackageRules.ManifestFile)
                {
                    if (entry.Length > MaxManifestBytes)
                    {
                        return Result.Fail(PackageErrors.Invalid("homemix.json may have at most 64 KB."));
                    }

                    using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                    manifest = reader.ReadToEnd();
                }
            }

            if (manifest is null)
            {
                return Result.Fail(PackageErrors.Invalid(
                    "homemix.json must be at the root of the zip (zip the files themselves, not the folder they are in)."));
            }

            return new PackageContents(manifest, files);
        }
        catch (InvalidDataException)
        {
            return Result.Fail(PackageErrors.Invalid("This is not a zip file."));
        }
    }
}
