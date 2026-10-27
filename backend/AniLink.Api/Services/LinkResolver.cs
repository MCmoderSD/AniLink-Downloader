using AniLink.Api.Data.Downloads;
using AniLink.Api.DebridLink;
using AniLink.Api.Exceptions.DebridLink;
using AniLink.Api.Helpers;

namespace AniLink.Api.Services;

// Asks Debrid-Link for the direct link of each part, one request at a time and in the order the parts were
// queued. That order is the point: every episode gets all of its links before the next one is started, so the
// first episode begins downloading while the later ones are still being resolved. A link that stopped
// working jumps the queue.
public sealed class LinkResolver(DebridLinkClient client, ILogger<LinkResolver> logger) : BackgroundService
{
    private const int MaxAttempts = 3;

    private readonly Lock _gate = new();
    private readonly PriorityQueue<Request, (int Priority, long Sequence)> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private long _sequence;

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (true)
            {
                await _signal.WaitAsync(stoppingToken);

                Request? request;
                lock (_gate) _queue.TryDequeue(out request, out _);

                // Cancelled while it was waiting in line: nothing to ask Debrid-Link for.
                if (request is null || request.Completion.Task.IsCompleted) continue;

                await ResolveOneAsync(request, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task ResolveOneAsync(Request request, CancellationToken stoppingToken)
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

                return;
            }
            catch (OperationCanceledException)
            {
                part.ResetForRetry();
                request.Completion.TrySetCanceled(request.Token);

                return;
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

                    return;
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Debrid-Link could not resolve {Url}", LogSafe.OneLine(part.Link.Url));

                part.Fail(exception.Message);
                request.Completion.TrySetException(exception);

                return;
            }
        }
    }
}
