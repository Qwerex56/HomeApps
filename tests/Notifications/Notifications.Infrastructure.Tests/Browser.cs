using System.Security.Cryptography;
using System.Text;
using Notifications.Domain.Recipients;

namespace Notifications.Infrastructure.Tests;

/// <summary>
/// The browser's side of Web Push: its subscription keys, and decrypting what arrives exactly as RFC 8291 and
/// RFC 8188 (aes128gcm) say, independently of the library the sender uses.
/// </summary>
internal sealed class Browser : IDisposable
{
    private readonly ECDiffieHellman _key = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
    private readonly byte[] _auth = RandomNumberGenerator.GetBytes(16);

    public byte[] PublicKey
    {
        get
        {
            var q = _key.ExportParameters(includePrivateParameters: false).Q;
            return [0x04, .. q.X!, .. q.Y!];
        }
    }

    public PushTarget Subscription(string endpoint = "https://fcm.googleapis.com/fcm/send/abc") =>
        PushTarget.Create(endpoint, Base64Url.Encode(PublicKey), Base64Url.Encode(_auth)).Value;

    public string Decrypt(byte[] body)
    {
        // Header: salt (16) | record size (4) | key id length (1) | key id = the sender's one-time public key.
        var salt = body[..16];
        int idLength = body[20];
        var senderPublic = body[21..(21 + idLength)];
        var record = body[(21 + idLength)..];

        using var sender = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = senderPublic[1..33], Y = senderPublic[33..65] },
        });
        var shared = _key.DeriveRawSecretAgreement(sender.PublicKey);

        // RFC 8291 3.4: the input key from the shared secret and the auth secret.
        byte[] keyInfo = [.. "WebPush: info\0"u8, .. PublicKey, .. senderPublic];
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, shared, 32, _auth, keyInfo);

        // RFC 8188 2.2 and 2.3: content key and nonce.
        var cek = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 16, salt, "Content-Encoding: aes128gcm\0"u8.ToArray());
        var nonce = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 12, salt, "Content-Encoding: nonce\0"u8.ToArray());

        var plain = new byte[record.Length - 16];
        using var aes = new AesGcm(cek, 16);
        aes.Decrypt(nonce, record[..^16], record[^16..], plain);

        // The last record ends with the delimiter 0x02, then padding zeros.
        var end = Array.LastIndexOf(plain, (byte)0x02);
        return Encoding.UTF8.GetString(plain, 0, end);
    }

    public void Dispose() => _key.Dispose();
}
