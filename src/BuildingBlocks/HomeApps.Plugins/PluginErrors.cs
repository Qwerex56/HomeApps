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
}
