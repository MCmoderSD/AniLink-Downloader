namespace AniLink.Api.Services;

public sealed class DownloadScheduler(DownloadOptions options)
{
    private readonly SemaphoreSlim _downloads = new(options.MaxConcurrency, options.MaxConcurrency);
    private readonly SemaphoreSlim _extractions = new(ExtractionLimit, ExtractionLimit);

    // 7-Zip unpacks a RAR archive on one thread, so each further archive can use one more. The limit is the number of
    // CPU threads the process has (a Docker CPU limit counts), and it is deliberately not a setting.
    public static int ExtractionLimit => Math.Max(1, Environment.ProcessorCount);

    public int ExtractionsRunning => ExtractionLimit - _extractions.CurrentCount;

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