using FluentResults;

namespace HomeApps.BuildingBlocks.Domain.Errors;

/// <summary>
/// Base of every typed error. The concrete type decides the HTTP status (see <c>ResultHttpMapper</c> in BuildingBlocks.Api);
/// <see cref="Code"/> is a stable, machine-readable identifier such as <c>user.not_found</c>.
/// Messages must be safe to show to a caller: never put secrets, passwords or tokens in them.
/// </summary>
public abstract class DomainError : Error
{
    protected DomainError(string code, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
        WithMetadata(nameof(Code), code);
    }

    public string Code { get; }
}

/// <summary>The requested thing does not exist (or the caller may not know it exists). Maps to 404.</summary>
public sealed class NotFoundError(string code, string message) : DomainError(code, message)
{
    public static NotFoundError For<TEntity>(object id) =>
        new($"{typeof(TEntity).Name.ToSnakeCase()}.not_found", $"{typeof(TEntity).Name} '{id}' was not found.");
}

/// <summary>The action clashes with the current state, e.g. a duplicate email. Maps to 409.</summary>
public sealed class ConflictError(string code, string message) : DomainError(code, message);

/// <summary>The caller is known but not allowed to do this. Maps to 403.</summary>
public sealed class ForbiddenError(string code, string message) : DomainError(code, message);

/// <summary>The caller could not be authenticated, e.g. wrong credentials. Maps to 401.</summary>
public sealed class UnauthorizedError(string code, string message) : DomainError(code, message);

/// <summary>
/// The input breaks a rule. Maps to 400 with a field → messages dictionary.
/// Use <see cref="Failures"/> for per-field messages; an empty key means the whole request.
/// </summary>
public sealed class ValidationError : DomainError
{
    public ValidationError(string code, string message, IReadOnlyDictionary<string, string[]>? failures = null)
        : base(code, message)
    {
        Failures = failures ?? new Dictionary<string, string[]>();
    }

    public IReadOnlyDictionary<string, string[]> Failures { get; }

    public static ValidationError ForField(string field, string message) =>
        new("validation.failed", message, new Dictionary<string, string[]> { [field] = [message] });
}
