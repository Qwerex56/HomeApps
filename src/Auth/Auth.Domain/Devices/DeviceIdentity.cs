using System.Security.Cryptography;
using System.Text;
using Auth.Domain.Common;

namespace Auth.Domain.Devices;

/// <summary>
/// A household satellite's own identity (jarvis-satellites.md 3a, S13): no email and no sign-in, only a secret handed
/// to the device once when it pairs. Its id is the device's id in Devices; tokens for it act for the household as a
/// member with the role Member. Pairing again replaces the secret; unpairing stops it until the next pairing.
/// </summary>
public sealed class DeviceIdentity
{
    public const int NameMaxLength = 20;

    private DeviceIdentity() { }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>SHA-256 of the secret; the secret has 256 random bits, so a fast hash is enough.</summary>
    public string SecretHash { get; private set; } = string.Empty;

    public DateTimeOffset PairedAt { get; private set; }

    public DateTimeOffset? UnpairedAt { get; private set; }

    public bool IsActive => UnpairedAt is null;

    /// <summary>The first pairing; the raw secret goes to the device and is never stored.</summary>
    public static (DeviceIdentity Identity, string Secret) Pair(Guid deviceId, Guid householdId, string name, DateTimeOffset now)
    {
        if (deviceId == Guid.Empty || householdId == Guid.Empty)
        {
            throw new ArgumentException("A device identity needs the device's and the household's ids.");
        }

        var identity = new DeviceIdentity { Id = deviceId };
        return (identity, identity.PairAgain(householdId, name, now));
    }

    /// <summary>New hardware or a new code: a new secret, the old one stops working.</summary>
    public string PairAgain(Guid householdId, string name, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var (raw, hash) = OpaqueToken.New();
        HouseholdId = householdId;
        Name = name.Length > NameMaxLength ? name[..NameMaxLength] : name;
        SecretHash = hash;
        PairedAt = now;
        UnpairedAt = null;
        return raw;
    }

    public void Unpair(DateTimeOffset now) => UnpairedAt ??= now;

    public bool Accepts(string secret) =>
        IsActive
        && !string.IsNullOrEmpty(secret)
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(OpaqueToken.HashOf(secret)), Encoding.ASCII.GetBytes(SecretHash));
}
