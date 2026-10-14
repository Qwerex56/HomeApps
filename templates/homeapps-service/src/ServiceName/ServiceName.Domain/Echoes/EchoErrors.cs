using HomeApps.BuildingBlocks.Domain.Errors;

namespace ServiceName.Domain.Echoes;

public static class EchoErrors
{
    public static readonly ValidationError InvalidMessage =
        ValidationError.ForField("Message", $"Message must be 1 to {Echo.MaxMessageLength} characters.");

    public static readonly ForbiddenError NotOwner = new("echo.not_owner", "Only the owner can see this echo.");

    public static NotFoundError NotFound(Guid id) => NotFoundError.For<Echo>(id);

    public static ConflictError AlreadyDelivered(Guid id) => new("echo.already_delivered", $"Echo '{id}' was already delivered.");
}
