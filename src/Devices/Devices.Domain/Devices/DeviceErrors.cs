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
        $"A device name is 1-{DeviceName.MaxLength} characters of any text, without control characters.",
        ErrorKind.Validation);

    public static Error InvalidAction => new AppError(
        "devices.invalid_action",
        $"An action needs a name of 1-{DeviceAction.MaxNameLength} characters (lowercase letters, digits, '_', starting with a letter), "
        + $"a description of at most {DeviceAction.MaxDescriptionLength} characters and an input schema that is a JSON object.",
        ErrorKind.Validation);

    public static Error InvalidDescriptor(IReadOnlyCollection<string> problems) => new AppError(
        "devices.invalid_descriptor",
        "The device descriptor (homeapps.device/1) is invalid: " + string.Join("; ", problems.Take(10)) + (problems.Count > 10 ? "; ..." : "."),
        ErrorKind.Validation);

    public static Error WrongPurpose => new AppError(
        "devices.wrong_purpose",
        "A household device is an assistant or a smart_home device; a person's own device is a computer.",
        ErrorKind.Validation);

    public static Error InvalidNotes => new AppError(
        "devices.invalid_notes", $"Notes for JARVIS are at most {Device.MaxNotesLength} characters.", ErrorKind.Validation);

    public static Error OwnerOnly => new AppError(
        "devices.owner_only", "Only the server's owner can manage registered capabilities.", ErrorKind.Forbidden);

    public static Error CapabilityNotFound => new AppError(
        "devices.capability_not_found", "There is no such registered capability.", ErrorKind.NotFound);

    public static Error TooManyActions => new AppError(
        "devices.too_many_actions", $"A device offers at most {Device.MaxActions} actions, each name once.", ErrorKind.Validation);

    public static Error ActionNotFound => new AppError("devices.action_not_found", "The device offers no such action.", ErrorKind.NotFound);

    public static Error NameTaken => new AppError(
        "devices.name_taken", "The household already has a device with this name.", ErrorKind.Conflict);

    public static Error Forbidden => new AppError(
        "devices.forbidden", "Only a household admin can change the household's devices.", ErrorKind.Forbidden);

    public static Error PairingExpired => new AppError(
        "devices.pairing_expired", "This pairing code is wrong, used or expired; ask a household admin for a new one.", ErrorKind.NotFound);

    public static Error NotPaired => new AppError(
        "devices.not_paired", "This device is not paired yet; pair it with the code from the household's device list.", ErrorKind.Conflict);

    public static Error Offline => new AppError(
        "devices.offline", "The device is not connected right now.", ErrorKind.Conflict);

    public static Error NoAnswer => new AppError(
        "devices.no_answer", "The device did not answer in time.", ErrorKind.Conflict);
}
