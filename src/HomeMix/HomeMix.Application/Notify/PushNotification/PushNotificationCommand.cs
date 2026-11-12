using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Notify.PushNotification;

/// <summary>
/// <c>notify.push</c> from an add-on (capability <c>notify</c>): a notification through Notifications to people of the
/// installation's household, or to the account's owner. <paramref name="UserIds"/> <c>null</c> means everyone else in
/// the household (or me, on an account). <paramref name="Path"/> is the add-on's own page that a click opens.
/// </summary>
public sealed record PushNotificationCommand(
    Guid InstallationId,
    IReadOnlyList<Guid>? UserIds,
    string Title,
    string Body,
    string? Path,
    string? Tag) : ICommand<PushNotificationResult>;
