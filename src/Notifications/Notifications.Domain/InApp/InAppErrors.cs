using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Notifications.Domain.InApp;

public static class InAppErrors
{
    /// <summary>Also for someone else's notification, so ids do not leak.</summary>
    public static Error NotFound => new AppError("notifications.in_app_not_found", "This notification does not exist.", ErrorKind.NotFound);
}
