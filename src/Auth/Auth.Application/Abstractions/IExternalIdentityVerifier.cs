using Auth.Domain.Identities;
using FluentResults;

namespace Auth.Application.Abstractions;

/// <summary>Exchanges an authorization code (with PKCE) for the provider's ID token and validates it (A-05, A-06).</summary>
public interface IExternalIdentityVerifier
{
    Task<Result<ExternalIdentity>> VerifyAsync(
        ExternalProvider provider, string code, string codeVerifier, Uri redirectUri, CancellationToken cancellationToken);
}

/// <summary>The person the provider vouches for. <see cref="Subject"/> is the provider's stable id.</summary>
public sealed record ExternalIdentity(
    string Subject, string Email, bool EmailVerified, string? FirstName, string? LastName, Uri? AvatarUrl);
