using System.ComponentModel.DataAnnotations;

namespace HomeApps.BuildingBlocks.Api.Auth;

/// <summary>
/// Configuration section <c>Jwt</c>. Services only validate tokens: they fetch the Auth service's public keys from
/// <c>{Authority}/.well-known/openid-configuration</c> (JWKS), so no signing secret lives in their config.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Base URL of the Auth service, also the expected <c>iss</c> claim unless <see cref="Issuer"/> is set.</summary>
    [Required]
    public string Authority { get; set; } = string.Empty;

    /// <summary>Expected <c>aud</c> claim.</summary>
    [Required]
    public string Audience { get; set; } = "homeapps";

    /// <summary>Expected <c>iss</c> claim when it differs from <see cref="Authority"/> (e.g. inside Docker networks).</summary>
    public string? Issuer { get; set; }

    /// <summary>Only turn off for local HTTP development.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
