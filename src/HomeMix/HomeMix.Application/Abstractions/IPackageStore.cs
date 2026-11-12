namespace HomeMix.Application.Abstractions;

/// <summary>The zips, one per SHA-256 (the <c>homemix-packages</c> volume).</summary>
public interface IPackageStore
{
    Task SaveAsync(string sha256, byte[] zip, CancellationToken cancellationToken);

    /// <summary>One file of a stored package, or <c>null</c> when the package or the file is not there.</summary>
    Task<Stream?> OpenFileAsync(string sha256, string path, CancellationToken cancellationToken);
}
