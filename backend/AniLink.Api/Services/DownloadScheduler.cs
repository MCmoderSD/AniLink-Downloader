namespace AniLink.Api.Services;

// Decides how much runs at once, and knows nothing about jobs. Downloads are limited by the configured
// concurrency; unpacking is one at a time, because it is bound by the disk and a second one only slows both.
public sealed class DownloadScheduler(DownloadOptions options)
{
    private readonly SemaphoreSlim _downloads = new(options.MaxConcurrency, options.MaxConcurrency);
    private readonly SemaphoreSlim _extractions = new(1, 1);

    public Task RunDownloadAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        return RunAsync(_downloads, work, cancellationToken);
    }

    public Task RunExtractionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        return RunAsync(_extractions, work, cancellationToken);
    }

    // Waiters are served in the order they arrived, which is what keeps the first episode ahead of the second.
    private static async Task RunAsync(SemaphoreSlim gate, Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);

        try
        {
            await work(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }
}
