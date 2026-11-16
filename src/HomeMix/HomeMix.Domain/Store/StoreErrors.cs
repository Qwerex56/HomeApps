using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Domain.Store;

public static class StoreErrors
{
    public static Error NotFound => new AppError("homemix.listing_not_found", "There is no such add-on in the store.", ErrorKind.NotFound);

    public static Error NotAuthor => new AppError(
        "homemix.not_author", "Only the person who submitted this add-on, or the instance's Owner, may change it.", ErrorKind.Forbidden);

    public static Error Official => new AppError(
        "homemix.listing_official", "Official add-ons are listed in addons.json; change them there.", ErrorKind.Validation);

    public static Error TooMany(int perDay) => new AppError(
        "homemix.store_limit", $"You can submit {perDay} add-ons a day; try again tomorrow.", ErrorKind.Conflict);

    /// <summary>GitHub does not describe a release that can be installed: <c>no_release</c>, <c>no_addon_zip</c>, <c>no_digest</c>, <c>unavailable</c>.</summary>
    public static Error Unusable(string problem) => new AppError(
        "homemix.store_unusable", $"This repository cannot be listed yet: {problem}.", ErrorKind.Validation)
        .WithMetadata(ProblemKey, problem);

    public const string ProblemKey = "problem";
}
