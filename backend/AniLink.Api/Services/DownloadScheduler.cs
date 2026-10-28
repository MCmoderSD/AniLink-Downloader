namespace AniLink.Api.Services;

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