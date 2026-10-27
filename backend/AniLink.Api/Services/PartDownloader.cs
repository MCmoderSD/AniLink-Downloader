using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using AniLink.Api.Data.Downloads;
using AniLink.Api.DebridLink;
using AniLink.Api.Exceptions.Downloads;
using AniLink.Api.Helpers;

namespace AniLink.Api.Services;

// Streams one part from its Debrid-Link URL to disk. The file grows as name.download and is renamed once it is
// complete, so an archive in archives/ is always a whole one. A leftover .download file from an earlier run is
// continued with a range request instead of starting over.
public sealed class PartDownloader(IHttpClientFactory httpClients, ILogger<PartDownloader> logger)
{
    private const int BufferSize = 128 * 1024;
    private const string PartialSuffix = ".download";

    private static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(200);

    public async Task DownloadAsync(ArchivePart part, CancellationToken cancellationToken)
    {
        var link = part.Debrid ?? throw new InvalidOperationException("The part has no download link yet.");
        var item = part.Item;

        var directory = item.Job.Workspace.ArchiveDirectory(item);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, part.StorageName);
        var partial = path + PartialSuffix;

        if (File.Exists(path) && part.BytesTotal > 0 && new FileInfo(path).Length == part.BytesTotal)
        {
            part.Downloaded(part.BytesTotal);
            return;
        }

        var existing = File.Exists(partial) ? new FileInfo(partial).Length : 0;

        using var request = new HttpRequestMessage(HttpMethod.Get, link.DownloadUrl);
        if (existing > 0) request.Headers.Range = new RangeHeaderValue(existing, null);

        using var response = await httpClients.CreateClient(DebridLinkClient.FileClient)
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode is HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            // Either the partial file already is the whole file, or it does not belong to this link.
            if (response.Content.Headers.ContentRange?.Length == existing)
            {
                File.Move(partial, path, true);
                part.Downloaded(existing);
                return;
            }

            File.Delete(partial);
            throw new IOException("The partial download did not match the file and was discarded.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new DownloadLinkException((int)response.StatusCode, $"The download link was refused with HTTP {(int)response.StatusCode}.");
        }

        var resumed = response.StatusCode is HttpStatusCode.PartialContent;
        if (!resumed) existing = 0;

        var contentLength = response.Content.Headers.ContentLength;
        var total = contentLength is { } length ? existing + length : Math.Max(link.Size, 0);
        var done = existing;

        part.BeginDownload(total, existing);

        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var target = new FileStream(partial, resumed ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, BufferSize, true))
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            var clock = Stopwatch.StartNew();

            try
            {
                int read;
                while ((read = await source.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    done += read;

                    if (clock.Elapsed < ReportInterval) continue;

                    clock.Restart();
                    part.Report(done);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        if (contentLength is not null && done != total)
        {
            throw new IOException($"The connection closed after {done} of {total} bytes.");
        }

        File.Move(partial, path, true);
        part.Downloaded(done);

        logger.LogInformation("Downloaded {File} ({Bytes} bytes)", LogSafe.OneLine(Path.GetFileName(path)), done);
    }
}
