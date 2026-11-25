namespace Notifications.Domain.InApp;

/// <summary>
/// One entry of a person's notification center (2.13): kept for <see cref="Retention"/> whether or not the person has
/// push on, so nothing is lost when Web Push is off. A new one with the same <see cref="Tag"/> replaces the unread one.
/// </summary>
public sealed class InAppNotification
{
    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    public const int MaxSourceLength = 50;
    public const int MaxCategoryLength = 50;
    public const int MaxTagLength = 100;
    public const int MaxTitleLength = 200;
    public const int MaxUrlLength = 500;

    private InAppNotification() { }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Source { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public string? Tag { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    public Guid? HouseholdId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static InAppNotification Create(
        Guid id, Guid userId, string source, string category, string? tag, string title, string body, string url, Guid? householdId, DateTimeOffset now) =>
        new()
        {
            Id = id,
            UserId = userId,
            Source = Cut(source, MaxSourceLength),
            Category = Cut(category, MaxCategoryLength),
            Tag = tag is null ? null : Cut(tag, MaxTagLength),
            Title = Cut(title, MaxTitleLength),
            Body = body,
            Url = Cut(url, MaxUrlLength),
            HouseholdId = householdId,
            CreatedAt = now,
        };

    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..max];
}
