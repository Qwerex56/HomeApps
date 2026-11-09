using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Notifications;

namespace Notifications.Application.Delivery.Deliver;

/// <summary>
/// Delivers a <see cref="NotificationRequested"/> from another service to the devices of everyone in it who wants it
/// (not muted, not in quiet hours). People without devices are skipped. Sent by the message consumer, not over HTTP.
/// </summary>
public sealed record DeliverNotificationCommand(NotificationRequested Request) : ICommand<DeliveryReport>;
