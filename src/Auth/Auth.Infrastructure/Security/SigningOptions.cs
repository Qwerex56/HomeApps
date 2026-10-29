using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Security;

/// <summary>Section <c>Auth:Signing</c> (Q9).</summary>
public sealed class SigningOptions
{
    public const string SectionName = "Auth:Signing";

    /// <summary>The <c>iss</c> of every token: Auth's public base URL, the <c>Jwt:Authority</c> of the other services.</summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>RSA private key in PEM, from a secret (Docker environment variable). Required outside Development.</summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>While changing keys: the old public key, still published and accepted until old tokens expire (15 min).</summary>
    public string? PreviousPublicKeyPem { get; set; }

    /// <summary>Development only: where the generated key is kept so tokens survive a restart. Relative to the content root.</summary>
    public string DevelopmentKeyPath { get; set; } = ".keys/auth-signing.pem";
}
