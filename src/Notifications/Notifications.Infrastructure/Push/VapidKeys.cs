using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Notifications.Infrastructure.Push;

/// <summary>
/// The VAPID keys in use. Production refuses to start without them. In Development, without configured keys, a
/// fresh pair is made on every start (browsers then have to turn notifications on again after a restart).
/// </summary>
public sealed partial class VapidKeys
{
    public VapidKeys(IOptions<VapidOptions> options, IHostEnvironment environment, ILogger<VapidKeys> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        var configured = options.Value;
        Subject = configured.Subject;

        if (!string.IsNullOrWhiteSpace(configured.PublicKey) && !string.IsNullOrWhiteSpace(configured.PrivateKey))
        {
            PublicKey = configured.PublicKey.Trim();
            PrivateKey = configured.PrivateKey.Trim();
            return;
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                $"Set {VapidOptions.SectionName}:PublicKey and PrivateKey (scripts/generate-vapid-keys.sh, docs/notifications.md).");
        }

        (PublicKey, PrivateKey) = Generate();
        LogGenerated(logger);
    }

    public string PublicKey { get; }

    public string PrivateKey { get; }

    public string Subject { get; }

    /// <summary>A new P-256 pair as base64url: the public key uncompressed (0x04 | X | Y), the private key as D.</summary>
    public static (string PublicKey, string PrivateKey) Generate()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(includePrivateParameters: true);
        var publicKey = new byte[65];
        publicKey[0] = 0x04;
        parameters.Q.X!.CopyTo(publicKey, 1);
        parameters.Q.Y!.CopyTo(publicKey, 33);
        return (Base64Url(publicKey), Base64Url(parameters.D!));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [LoggerMessage(Level = LogLevel.Warning, Message = "No VAPID keys configured; using a temporary pair until the next start")]
    private static partial void LogGenerated(ILogger logger);
}
