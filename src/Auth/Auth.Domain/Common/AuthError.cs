using FluentResults;

namespace Auth.Domain.Common;

/// <summary>
/// Builds a domain error with a stable machine-readable code (auth.md section 8) in its metadata.
/// Messages are safe to show to a caller: never put passwords, tokens or other input in them.
/// </summary>
public static class AuthError
{
    /// <summary>Metadata key holding the error code, e.g. <c>email.taken</c>.</summary>
    public const string CodeKey = "Code";

    public static Error Create(string code, string message) => new Error(message).WithMetadata(CodeKey, code);
}
