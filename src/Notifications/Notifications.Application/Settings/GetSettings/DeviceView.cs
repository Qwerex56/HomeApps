namespace Notifications.Application.Settings.GetSettings;

/// <param name="Endpoint">So the browser recognises itself in the list.</param>
public sealed record DeviceView(Guid Id, string? UserAgent, string Endpoint, DateTimeOffset RegisteredAt, DateTimeOffset? LastUsedAt);
