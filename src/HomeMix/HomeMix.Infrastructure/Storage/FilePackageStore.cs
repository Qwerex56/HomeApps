using System.IO.Compression;
using HomeMix.Application;
using HomeMix.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace HomeMix.Infrastructure.Storage;

/// <summary>
/// The zips as files named by their SHA-256 (<c>{PackagesPath}/ab/abcd....zip</c>). A file is written once and never
/// changed, so serving needs no locks; files are read straight out of the zip (they are small).
/// </summary>
internal sealed class FilePackageStore(IOptions<HomeMixOptions> options) : IPackageStore
{
    public async Task SaveAsync(string sha256, byte[] zip, CancellationToken cancellationToken)
    {
        var path = PathOf(sha256);
        if (File.Exists(path))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temporary, zip, cancellationToken);
        File.Move(temporary, path, overwrite: true);
    }

    public async Task<Stream?> OpenFileAsync(string sha256, string path, CancellationToken cancellationToken)
    {
        var file = PathOf(sha256);
        if (!File.Exists(file))
        {
            return null;
        }

        await using var zip = File.OpenRead(file);
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read);
        var entry = archive.GetEntry(path);
        if (entry is null || entry.FullName.EndsWith('/'))
        {
            return null;
        }

        var copy = new MemoryStream((int)Math.Min(entry.Length, int.MaxValue));
        await using (var content = entry.Open())
        {
            await content.CopyToAsync(copy, cancellationToken);
        }

        copy.Position = 0;
        return copy;
    }

    private string PathOf(string sha256)
    {
        if (sha256.Length != 64 || !sha256.All(char.IsAsciiHexDigitLower))
        {
            throw new ArgumentException("A package is named by its lowercase SHA-256.", nameof(sha256));
        }

        return Path.Combine(options.Value.PackagesPath, sha256[..2], $"{sha256}.zip");
    }
}
