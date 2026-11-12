using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeApps.Plugins;

public static class PluginErrors
{
    /// <summary>Same code as Households: no access looks the same as no household (Q3).</summary>
    public static Error HouseholdNotFound => new AppError("household.not_found", "The household does not exist.", ErrorKind.NotFound);

    /// <summary>The web app offers to switch the plugin on when it sees this code.</summary>
    public static Error Disabled => new AppError("plugin.disabled", "This app is not switched on in the household.", ErrorKind.NotFound);

    public static Error Forbidden => new AppError("plugin.forbidden", "Your role in this household does not allow this.", ErrorKind.Forbidden);

    /// <summary>
    /// The plugin is off in the household, or the caller's role may not do this: a tool that looks through all of
    /// someone's households skips such a household instead of failing.
    /// </summary>
    public static bool IsOffOrForbidden(IEnumerable<IError> errors) =>
        errors.OfType<AppError>().Any(e => e.Code is "plugin.disabled" or "plugin.forbidden");
}
