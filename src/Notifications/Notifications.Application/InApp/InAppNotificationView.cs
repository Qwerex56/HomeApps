namespace Notifications.Application.InApp;

public sealed record InAppNotificationView(
    Guid Id, string Source, string Category, string? Tag, string Title, string Body, string Url, Guid? HouseholdId, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);
