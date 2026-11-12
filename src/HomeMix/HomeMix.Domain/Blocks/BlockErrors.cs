using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Domain.Blocks;

public static class BlockErrors
{
    public static Error NotFound => new AppError("homemix.block_not_found", "This add-on is not blocked here.", ErrorKind.NotFound);

    /// <summary>Blocks from <c>HomeMix:Blocked</c> are lifted in the configuration, not in the web app.</summary>
    public static Error InConfiguration => new AppError(
        "homemix.block_in_configuration", "This block comes from the server's configuration (HomeMix:Blocked); remove it there.", ErrorKind.Conflict);
}
