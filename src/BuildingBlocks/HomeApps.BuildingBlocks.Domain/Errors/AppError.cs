using FluentResults;

namespace HomeApps.BuildingBlocks.Domain.Errors;

/// <summary>What went wrong, in terms the API turns into an HTTP status (see <c>ResultHttpMapper</c>).</summary>
public enum ErrorKind
{
    /// <summary>400: the input breaks a rule.</summary>
    Validation = 0,

    /// <summary>401: the caller could not prove who they are.</summary>
    Unauthorized = 1,

    /// <summary>403: the caller is known but not allowed to do this.</summary>
    Forbidden = 2,

    /// <summary>404: the thing does not exist, or the caller may not know it does.</summary>
    NotFound = 3,

    /// <summary>409: the action clashes with the current state, e.g. a duplicate email.</summary>
    Conflict = 4,

    /// <summary>423: temporarily locked, e.g. after too many failed sign-ins.</summary>
    Locked = 5,
}

/// <summary>
/// A domain or application error with a stable machine-readable <see cref="Code"/> (e.g. <c>email.taken</c>) and a
/// <see cref="Kind"/>. One type on purpose: no error class hierarchy (ADR 0002). Messages must be safe to show to a
/// caller: never put passwords, tokens or other secrets in them.
/// </summary>
public sealed class AppError : Error
{
    /// <summary>Metadata key holding <see cref="Code"/>, so plain FluentResults code can read it too.</summary>
    public const string CodeKey = "Code";

    public AppError(string code, string message, ErrorKind kind)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
        Kind = kind;
        Metadata.Add(CodeKey, code);
    }

    public string Code { get; }

    public ErrorKind Kind { get; }
}
