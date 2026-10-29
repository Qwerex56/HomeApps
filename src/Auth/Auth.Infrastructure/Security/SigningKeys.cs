using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Security;

/// <summary>
/// The RSA key that signs tokens and the public keys others validate with (A-31). The key id is the RFC 7638
/// thumbprint, so the same key always gets the same <c>kid</c>.
/// </summary>
public sealed class SigningKeys : IDisposable
{
    private readonly RSA _current;
    private readonly RSA? _previous;

    public SigningKeys(IOptions<SigningOptions> options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        _current = RSA.Create();
        _current.ImportFromPem(LoadPrivateKeyPem(options.Value, environment));
        CurrentKey = WithKeyId(new RsaSecurityKey(_current));
        SigningCredentials = new SigningCredentials(CurrentKey, SecurityAlgorithms.RsaSha256);

        var validation = new List<SecurityKey> { PublicOnly(_current) };
        if (!string.IsNullOrWhiteSpace(options.Value.PreviousPublicKeyPem))
        {
            _previous = RSA.Create();
            _previous.ImportFromPem(options.Value.PreviousPublicKeyPem);
            validation.Add(PublicOnly(_previous));
        }

        ValidationKeys = validation;
        Jwks = new JsonWebKeySet();
        foreach (var key in validation)
        {
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey((RsaSecurityKey)key);
            jwk.Use = JsonWebKeyUseNames.Sig;
            jwk.Alg = SecurityAlgorithms.RsaSha256;
            Jwks.Keys.Add(jwk);
        }
    }

    public RsaSecurityKey CurrentKey { get; }

    public SigningCredentials SigningCredentials { get; }

    /// <summary>Public keys tokens may be signed with: the current one and, while changing keys, the previous one.</summary>
    public IReadOnlyList<SecurityKey> ValidationKeys { get; }

    /// <summary>What <c>/.well-known/jwks.json</c> serves.</summary>
    public JsonWebKeySet Jwks { get; }

    public void Dispose()
    {
        _current.Dispose();
        _previous?.Dispose();
    }

    private static string LoadPrivateKeyPem(SigningOptions options, IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(options.PrivateKeyPem))
        {
            return options.PrivateKeyPem;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"{SigningOptions.SectionName}:{nameof(SigningOptions.PrivateKeyPem)} is required outside Development.");
        }

        var path = Path.Combine(environment.ContentRootPath, options.DevelopmentKeyPath);
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }

        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportPkcs8PrivateKeyPem();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, pem);
        return pem;
    }

    private static RsaSecurityKey PublicOnly(RSA rsa) => WithKeyId(new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false)));

    private static RsaSecurityKey WithKeyId(RsaSecurityKey key)
    {
        var publicKey = new RsaSecurityKey(key.Rsa?.ExportParameters(false) ?? key.Parameters);
        key.KeyId = Base64UrlEncoder.Encode(JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey).ComputeJwkThumbprint());
        return key;
    }
}
