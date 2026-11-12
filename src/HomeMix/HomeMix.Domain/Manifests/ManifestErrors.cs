using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Domain.Manifests;

public static class ManifestErrors
{
    public static Error Invalid(string problem) => new AppError("homemix.manifest_invalid", problem, ErrorKind.Validation);

    public static Error ReservedId(string id) => new AppError(
        "homemix.id_reserved", $"The id '{id}' belongs to a plugin or a part of HomeApps; choose another.", ErrorKind.Validation);

    public static Error NeedsNewerHomeApps(string required, string current) => new AppError(
        "homemix.needs_newer_homeapps", $"This add-on needs HomeApps {required}; this instance runs {current}.", ErrorKind.Validation);
}
