using System.Diagnostics;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Repositories;
using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services;

// Turns the constant stream of tiny changes into one snapshot per job every half second, and measures the
// download speed while at it. A job that did not change since the last tick is not sent again.
public sealed class ProgressPublisher(JobRepository jobs, ProgressHub hub) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(500);

    private readonly Dictionary<Guid, Seen> _seen = [];

    private sealed record Seen(long Revision, long Bytes, long Stamp, double Speed, bool Moving);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken)) Tick();
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private void Tick()
    {
        var now = Stopwatch.GetTimestamp();
        var live = new HashSet<Guid>();

        foreach (var job in jobs.List())
        {
            live.Add(job.Id);

            var revision = job.Revision;
            var bytes = job.BytesDone;
            var previous = _seen.GetValueOrDefault(job.Id);

            var speed = 0d;

            if (previous is not null && job.Status is JobStatus.Active)
            {
                var seconds = Stopwatch.GetElapsedTime(previous.Stamp, now).TotalSeconds;
                var instant = seconds > 0 ? Math.Max(bytes - previous.Bytes, 0) / seconds : 0;

                // Smoothed, so the number on screen does not jitter with every burst of the network.
                speed = previous.Speed * 0.6 + instant * 0.4;
                if (speed < 1) speed = 0;
            }

            job.SpeedBytesPerSecond = speed;

            var moving = speed > 0;
            var changed = previous is null || previous.Revision != revision || previous.Moving != moving;

            if (changed && hub.HasListeners) hub.Publish(DownloadEvents.Job(job));

            _seen[job.Id] = new Seen(revision, bytes, now, speed, moving);
        }

        foreach (var gone in _seen.Keys.Where(id => !live.Contains(id)).ToList()) _seen.Remove(gone);
    }
}
