namespace Notifications.Application.Delivery;

/// <param name="Delivered">Devices the push service accepted the notification for.</param>
/// <param name="Gone">Devices removed because their subscription expired.</param>
/// <param name="Skipped">People who got nothing: no devices, muted or quiet hours.</param>
public sealed record DeliveryReport(int Delivered, int Gone, int Failed, int Skipped)
{
    public static DeliveryReport Empty { get; } = new(0, 0, 0, 0);

    public DeliveryReport Add(DeliveryReport other) =>
        new(Delivered + other.Delivered, Gone + other.Gone, Failed + other.Failed, Skipped + other.Skipped);
}
