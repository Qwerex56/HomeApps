namespace HomeMix.Application.Realtime;

/// <summary>
/// The events of one request, sent by <see cref="RealtimeBehavior{TRequest, TResponse}"/> only after the command
/// committed, so a browser that reads again on an event reads the new state.
/// </summary>
public sealed class RealtimeOutbox
{
    private readonly List<RealtimeMessage> _messages = [];

    public void Add(RealtimeMessage message) => _messages.Add(message);

    public IReadOnlyList<RealtimeMessage> TakeAll()
    {
        var all = _messages.ToList();
        _messages.Clear();
        return all;
    }
}
