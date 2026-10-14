using FluentResults;
using HomeApps.BuildingBlocks.Domain;

namespace ServiceName.Domain.Echoes;

/// <summary>
/// Sample aggregate. The rules live here: handlers only load it, call one method and return.
/// </summary>
public sealed class Echo : AggregateRoot<Guid>
{
    public const int MaxMessageLength = 200;

    private Echo()
    {
        Message = string.Empty;
    }

    private Echo(Guid id, Guid ownerId, string message, DateTimeOffset createdAt)
        : base(id)
    {
        OwnerId = ownerId;
        Message = message;
        CreatedAt = createdAt;
    }

    public Guid OwnerId { get; private set; }

    public string Message { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public static Result<Echo> Create(Guid ownerId, string message, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Length > MaxMessageLength)
        {
            return Result.Fail(EchoErrors.InvalidMessage);
        }

        var echo = new Echo(Guid.NewGuid(), ownerId, message.Trim(), now);
        echo.Raise(new EchoCreated(echo.Id, ownerId, echo.Message));
        return echo;
    }

    public Result MarkDelivered(DateTimeOffset now)
    {
        if (DeliveredAt is not null)
        {
            return Result.Fail(EchoErrors.AlreadyDelivered(Id));
        }

        DeliveredAt = now;
        return Result.Ok();
    }

    public Result EnsureOwnedBy(Guid userId) =>
        OwnerId == userId ? Result.Ok() : Result.Fail(EchoErrors.NotOwner);
}
