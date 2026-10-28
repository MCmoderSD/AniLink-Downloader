using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AniLink.Api.Services.Streaming;

public sealed class StreamSubscription(ChannelReader<string> reader, Action release) : IDisposable
{
    public ChannelReader<string> Reader { get; } = reader;

    public void Dispose()
    {
        release();
    }
}

public class StreamHub
{
    private const int Backlog = 256;

    private readonly ConcurrentDictionary<Guid, Channel<string>> _listeners = new();

    public bool HasListeners => !_listeners.IsEmpty;

    public StreamSubscription Subscribe()
    {
        var id = Guid.NewGuid();
        var queue = Channel.CreateBounded<string>(new BoundedChannelOptions(Backlog)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

        _listeners[id] = queue;

        return new StreamSubscription(queue.Reader, () => Release(id));
    }

    public void Publish(string payload)
    {
        foreach (var (_, queue) in _listeners) queue.Writer.TryWrite(payload);
    }

    private void Release(Guid id)
    {
        _listeners.TryRemove(id, out _);
    }
}

public sealed class ProgressHub : StreamHub;