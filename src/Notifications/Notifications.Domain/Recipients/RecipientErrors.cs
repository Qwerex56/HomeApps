using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Notifications.Domain.Recipients;

public static class RecipientErrors
{
    public static Error DeviceNotFound => new AppError("notifications.device_not_found", "This device is not on the list.", ErrorKind.NotFound);

    public static Error NoDevices => new AppError(
        "notifications.no_devices", "Notifications are not turned on on any device.", ErrorKind.Conflict);

    public static Error InvalidEndpoint => new AppError(
        "notifications.invalid_endpoint", "The push subscription needs an https endpoint of at most 1000 characters.", ErrorKind.Validation);

    public static Error InvalidKeys => new AppError(
        "notifications.invalid_keys", "The push subscription's keys are not valid (p256dh: 65 bytes, auth: 16 bytes, base64url).", ErrorKind.Validation);

    public static Error QuietHoursEmpty => new AppError(
        "notifications.quiet_hours_empty", "Quiet hours must start and end at different times.", ErrorKind.Validation);

    public static Error UnknownTimeZone => new AppError("notifications.unknown_time_zone", "Unknown time zone.", ErrorKind.Validation);

    public static Error InvalidMute => new AppError(
        "notifications.invalid_mute", "A mute needs a household and at most a 32-character source.", ErrorKind.Validation);
}
