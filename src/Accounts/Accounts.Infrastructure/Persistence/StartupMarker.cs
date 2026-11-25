namespace Accounts.Infrastructure.Persistence;

/// <summary>A one-off start-up job that has run, so a restart does not repeat it.</summary>
public sealed class StartupMarker
{
    private StartupMarker() { }

    public StartupMarker(string name, DateTimeOffset doneAt)
    {
        Name = name;
        DoneAt = doneAt;
    }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset DoneAt { get; private set; }
}
