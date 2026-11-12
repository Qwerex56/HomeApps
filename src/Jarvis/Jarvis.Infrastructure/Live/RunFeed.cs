using System.Collections.Concurrent;
using System.Threading.Channels;
using Jarvis.Application.Abstractions;

namespace Jarvis.Infrastructure.Live;

/// <summary>
/// In memory, in this process: the brain and the gateway run here, and so does the request that watches. A stream is
/// opened before asking, linked to the run once it starts, and closed when the answer is out.
/// </summary>
public sealed class RunFeed : IRunFeed
{
    private readonly ConcurrentDictionary<Guid, Channel<RunUpdate>> _streams = new();
    private readonly ConcurrentDictionary<Guid, Guid> _runs = new();

    public ChannelReader<RunUpdate> Open(Guid streamId) =>
        _streams.GetOrAdd(streamId, _ => Channel.CreateUnbounded<RunUpdate>(new UnboundedChannelOptions { SingleReader = true })).Reader;

    public void Close(Guid streamId)
    {
        if (_streams.TryRemove(streamId, out var stream))
        {
            stream.Writer.TryComplete();
        }

        foreach (var run in _runs.Where(r => r.Value == streamId).Select(r => r.Key).ToList())
        {
            _runs.TryRemove(run, out _);
        }
    }

    public void Link(Guid streamId, Guid runId)
    {
        if (_streams.ContainsKey(streamId))
        {
            _runs[runId] = streamId;
        }
    }

    public void Publish(Guid runId, RunUpdate update)
    {
        if (_runs.TryGetValue(runId, out var streamId) && _streams.TryGetValue(streamId, out var stream))
        {
            stream.Writer.TryWrite(update);
        }
    }
}
