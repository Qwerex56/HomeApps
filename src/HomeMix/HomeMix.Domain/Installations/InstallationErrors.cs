using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Domain.Installations;

public static class InstallationErrors
{
    public static Error NotFound => new AppError("homemix.installation_not_found", "This add-on is not installed here.", ErrorKind.NotFound);

    public static Error Forbidden => new AppError(
        "homemix.forbidden", "Only a household's admins can add or change its add-ons; an account's add-ons only its owner.", ErrorKind.Forbidden);

    public static Error AlreadyInstalled => new AppError(
        "homemix.already_installed", "This add-on is already installed here; update it instead.", ErrorKind.Conflict);

    public static Error TargetNotSupported(TargetKind kind) => new AppError(
        "homemix.target_not_supported",
        kind == TargetKind.Household ? "This add-on can only be installed on an account." : "This add-on can only be installed in a household.",
        ErrorKind.Validation);

    public static Error OtherAddon => new AppError(
        "homemix.other_addon", "This package is a different add-on; install it separately.", ErrorKind.Validation);

    /// <summary>The installer has to accept these first (a new install, or an update that asks for more).</summary>
    public static Error ConsentRequired(IEnumerable<string> missing) => new AppError(
        "homemix.consent_required", $"Accept first: {string.Join(", ", missing)}.", ErrorKind.Conflict);
}
