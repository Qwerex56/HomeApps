namespace HomeApps.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Thrown by <see cref="IUnitOfWork.SaveChangesAsync"/> when another request changed the same row first (optimistic
/// concurrency). Nothing was saved. A handler that expects races may catch it and return a typed error; otherwise the
/// API answers 409 <c>concurrency.conflict</c> and the client may retry.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
