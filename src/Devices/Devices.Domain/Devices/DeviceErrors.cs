using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Devices.Domain.Devices;

public static class DeviceErrors
{
    public static Error NotFound => new AppError("devices.not_found", "There is no such device.", ErrorKind.NotFound);

    public static Error Disabled => new AppError(
        "devices.disabled", "This device was removed; add it again to use it.", ErrorKind.Conflict);

    public static Error InvalidName => new AppError(
        "devices.invalid_name",
        $"A device name is 1-{Device.MaxNameLength} characters: lowercase letters, digits and '-', starting with a letter or digit.",
        ErrorKind.Validation);

    public static Error InvalidAction => new AppError(
        "devices.invalid_action",
        $"An action needs a name of 1-{DeviceAction.MaxNameLength} characters (lowercase letters, digits, '_', starting with a letter), "
        + $"a description of at most {DeviceAction.MaxDescriptionLength} characters and an input schema that is a JSON object.",
        ErrorKind.Validation);

    public static Error TooManyActions => new AppError(
        "devices.too_many_actions", $"A device offers at most {Device.MaxActions} actions, each name once.", ErrorKind.Validation);

    public static Error ActionNotFound => new AppError("devices.action_not_found", "The device offers no such action.", ErrorKind.NotFound);

    public static Error Offline => new AppError(
        "devices.offline", "The device is not connected right now.", ErrorKind.Conflict);

    public static Error NoAnswer => new AppError(
        "devices.no_answer", "The device did not answer in time.", ErrorKind.Conflict);
}
