using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Api.Mcp;

public static class ToolErrors
{
    public static Error Unknown(string name) => new AppError("homemix.tool_not_found", $"There is no tool named {name}.", ErrorKind.NotFound);

    public static Error Argument(string problem) => new AppError("homemix.tool_argument", problem, ErrorKind.Validation);
}
