using System.Diagnostics;
using System.Threading.Channels;
using AniLink.Api.Helpers;
using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services.Files;

public sealed class FileChanges(FilePaths paths, FolderTotals totals, FileHub hub) : BackgroundService
{
    private const int MaxPaths = 64;

    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan Spacing = TimeSpan.FromMilliseconds(1500);

    private readonly Lock _sync = new();
    private readonly Channel<bool> _signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly HashSet<string> _pending = new(PathScope.Comparer);
    private bool _reset;
    private long _published;

    public void Report(string fullPath)
    {
        if (FilePaths.IsInternal(Path.GetFileName(fullPath))) return;

        lock (_sync)
        {
            if (!_reset)
            {
                _pending.Add(fullPath);

                if (_pending.Count > MaxPaths) MarkReset();
            }
        }

        _signal.Writer.TryWrite(true);
    }

    public void Reset()
    {
        lock (_sync) MarkReset();

        _signal.Writer.TryWrite(true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await _signal.Reader.WaitToReadAsync(stoppingToken))
            {
                _signal.Reader.TryRead(out _);

                await Task.Delay(Quiet, stoppingToken);

                var wait = Spacing - Stopwatch.GetElapsedTime(_published);
                if (wait > TimeSpan.Zero) await Task.Delay(wait, stoppingToken);

                Publish();
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private void MarkReset()
    {
        _reset = true;
        _pending.Clear();
    }

    private void Publish()
    {
        string[] changed;
        bool reset;

        lock (_sync)
        {
            reset = _reset;
            changed = [.. _pending];

            _reset = false;
            _pending.Clear();
        }

        if (!reset && changed.Length == 0) return;

        _published = Stopwatch.GetTimestamp();

        if (reset) totals.Clear();
        else foreach (var path in changed) totals.Invalidate(path);

        if (!hub.HasListeners) return;

        hub.Publish(reset ? FileEvents.Reset() : FileEvents.Changed(changed.Select(Relative)));
    }

    private string Relative(string fullPath)
    {
        var relative = Path.GetRelativePath(paths.Root, fullPath);

        return relative == "." ? "" : relative.Replace(Path.DirectorySeparatorChar, '/');
    }
}