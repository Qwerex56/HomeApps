using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Jarvis.Domain.ToolCalls;

public static class ToolCallErrors
{
    /// <summary>Also for someone else's: only the person who asked sees and confirms it (JV9).</summary>
    public static Error NotFound => new AppError("jarvis.action_not_found", "There is no such action.", ErrorKind.NotFound);

    public static Error NotPending => new AppError("jarvis.action_not_pending", "This action does not wait for a confirmation.", ErrorKind.Conflict);

    public static Error Expired => new AppError("jarvis.action_expired", "Expired: ask JARVIS again.", ErrorKind.Conflict);
}
