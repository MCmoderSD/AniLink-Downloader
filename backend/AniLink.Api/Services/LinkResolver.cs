using AniLink.Api.Data.Downloads;
using AniLink.Api.DebridLink;
using AniLink.Api.Exceptions.DebridLink;
using AniLink.Api.Helpers;

namespace AniLink.Api.Services;

public sealed class LinkResolver(DebridLinkClient client, LinkPace pace, ILogger<LinkResolver> logger) : BackgroundService
{
    private const int MaxAttempts = 3;

    private readonly Lock _gate = new();
    private readonly PriorityQueue<Request, (int Priority, long Sequence)> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private long _sequence;
    private Request? _current;

    private sealed record Request(ArchivePart Part, TaskCompletionSource Completion, CancellationToken Token);

    public Task ResolveAsync(ArchivePart part, bool urgent, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);

        var request = new Request(part, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously), cancellationToken);

        var registration = cancellationToken.Register(() => request.Completion.TrySetCanceled(cancellationToken));
        _ = request.Completion.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);

        lock (_gate) _queue.Enqueue(request, (urgent ? 0 : 1, _sequence++));
        _signal.Release();

        return request.Completion.Task;
    }

    public int LinksAhead(DownloadJob job)
    {
        lock (_gate)
        {
            var running = _current is { Completion.Task.IsCompleted: false } ? _current : null;
            var waiting = _queue.UnorderedItems.Where(entry => !entry.Element.Completion.Task.IsCompleted).ToList();
            var own = waiting.Where(entry => entry.Element.Part.Item.Job == job).ToList();

            if (own.Count == 0) return running?.Part.Item.Job == job ? 1 : 0;

            var last = own.Max(entry => entry.Priority);

            return waiting.Count(entry => Comparer<(int Priority, long Sequence)>.Default.Compare(entry.Priority, last) <= 0) + (running is null ? 0 : 1);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (true)
            {
                await _signal.WaitAsync(stoppingToken);

                Request? request;

                lock (_gate)
                {
                    _queue.TryDequeue(out request, out _);
                    _current = request;
                }

                if (request is not null && !request.Completion.Task.IsCompleted && await ResolveOneAsync(request, stoppingToken)) pace.Finished();

                bool empty;

                lock (_gate)
                {
                    _current = null;
                    empty = _queue.Count == 0;
                }

                if (empty) pace.Idle();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task<bool> ResolveOneAsync(Request request, CancellationToken stoppingToken)
    {
        var part = request.Part;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, request.Token);

        part.BeginResolving();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var file = await client.AddAsync(part.Link.Url, linked.Token);

                part.Resolved(file);
                request.Completion.TrySetResult();

                return true;
            }
            catch (OperationCanceledException)
            {
                part.ResetForRetry();
                request.Completion.TrySetCanceled(request.Token);

                return false;
            }
            catch (DebridLinkException exception) when (exception.Retryable && attempt < MaxAttempts)
            {
                logger.LogWarning("Debrid-Link failed for {Url} ({Code}), attempt {Attempt} of {Max}", LogSafe.OneLine(part.Link.Url), exception.Code, attempt, MaxAttempts);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5 * attempt), linked.Token);
                }
                catch (OperationCanceledException)
                {
                    part.ResetForRetry();
                    request.Completion.TrySetCanceled(request.Token);

                    return false;
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Debrid-Link could not resolve {Url}", LogSafe.OneLine(part.Link.Url));

                part.Fail(exception.Message);
                request.Completion.TrySetException(exception);

                return true;
            }
        }
    }
}