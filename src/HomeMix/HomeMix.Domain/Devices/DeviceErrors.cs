using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace HomeMix.Domain.Devices;

public static class DeviceErrors
{
    public static Error HouseholdOnly => new AppError(
        "homemix.devices_household_only", "Devices belong to a household: install the add-on in a household to use them.", ErrorKind.Validation);

    /// <summary>The device is not in the installation's household (or does not exist): an add-on never reaches another household's devices.</summary>
    public static Error NotFound => new AppError("homemix.device_not_found", "There is no such device in this household.", ErrorKind.NotFound);

    public static Error NotAMatch(string widgetId) => new AppError(
        "homemix.device_not_a_match", $"This device does not match what the widget '{widgetId}' asks for.", ErrorKind.Validation);
}
