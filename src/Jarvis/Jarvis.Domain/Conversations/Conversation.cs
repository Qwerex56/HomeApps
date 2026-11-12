namespace Jarvis.Domain.Conversations;

/// <summary>
/// A person's own conversation with JARVIS in the app (jarvis-v1.md 2.4). Only its owner sees it, the server's Owner too
/// cannot. The title comes from the first message until the person names it.
/// </summary>
public sealed class Conversation
{
    public const string DefaultTitle = "Nowa rozmowa";
    public const int TitleLength = 80;

    private Conversation()
    {
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; } = DefaultTitle;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset LastActivityAt { get; private set; }

    public static Conversation Start(Guid ownerId, string? title, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        var now = time.GetUtcNow();
        return new Conversation
        {
            Id = Guid.CreateVersion7(now),
            OwnerId = ownerId,
            Title = string.IsNullOrWhiteSpace(title) ? DefaultTitle : Shorten(title),
            StartedAt = now,
            LastActivityAt = now,
        };
    }

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;

    /// <summary>The person wrote: the conversation moves up the list, and an unnamed one takes the first words as title.</summary>
    public ConversationMessage Write(string text, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (Title == DefaultTitle)
        {
            Title = Shorten(text);
        }

        LastActivityAt = time.GetUtcNow();
        return ConversationMessage.Create(Id, MessageRole.Person, text, null, time);
    }

    public ConversationMessage Answer(string text, Guid runId, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        LastActivityAt = time.GetUtcNow();
        return ConversationMessage.Create(Id, MessageRole.Assistant, text, runId, time);
    }

    private static string Shorten(string text)
    {
        var line = text.Trim().ReplaceLineEndings(" ");
        return line.Length <= TitleLength ? line : string.Concat(line.AsSpan(0, TitleLength - 1), "…");
    }
}
