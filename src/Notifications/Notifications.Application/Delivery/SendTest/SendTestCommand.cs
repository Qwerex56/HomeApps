using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.Delivery.SendTest;

/// <summary>"Send a test notification" in the settings: to every device of mine at once, ignoring mutes and quiet hours.</summary>
public sealed record SendTestCommand : ICommand<DeliveryReport>;
