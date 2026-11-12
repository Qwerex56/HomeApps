using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Domain.Packages;

public static class PackageErrors
{
    public static Error Invalid(string problem) => new AppError("homemix.package_invalid", problem, ErrorKind.Validation);

    public static Error TooLarge(long maxBytes) => new AppError(
        "homemix.package_too_large", $"An add-on package may have at most {maxBytes / (1024 * 1024)} MB.", ErrorKind.Validation);

    public static Error NotFound => new AppError("homemix.package_not_found", "There is no such add-on package; add it again.", ErrorKind.NotFound);

    public static Error Blocked => new AppError(
        "homemix.blocked", "The owner of this HomeApps instance blocked this add-on.", ErrorKind.Forbidden);

    public static Error UploadsDisabled => new AppError(
        "homemix.uploads_disabled", "Adding add-ons is switched off on this HomeApps instance.", ErrorKind.Forbidden);

    public static Error Download(string problem) => new AppError("homemix.download_failed", problem, ErrorKind.Validation);
}
