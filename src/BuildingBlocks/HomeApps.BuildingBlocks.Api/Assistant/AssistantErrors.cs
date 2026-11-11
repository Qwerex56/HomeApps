using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeApps.BuildingBlocks.Api.Assistant;

public static class AssistantErrors
{
    public static AppError NotAllowed { get; } = new(
        "assistant.not_allowed",
        "An assistant cannot do this on a person's behalf; the person has to do it in the app.",
        ErrorKind.Forbidden);
}
