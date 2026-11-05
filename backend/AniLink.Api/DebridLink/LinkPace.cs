namespace AniLink.Api.DebridLink;

public sealed class LinkPace(DebridLinkOptions options)
{
    private const long Window = 60_000;
    private const long LongestStep = 30_000;
    private const int PriorWeight = 3;

    private readonly Lock _gate = new();
    private readonly Queue<long> _finished = new();

    public double SecondsEach
    {
        get
        {
            lock (_gate)
            {
                var sum = 0L;
                long? before = null;

                foreach (var at in _finished)
                {
                    if (before is not null) sum += Math.Min(at - before.Value, LongestStep);
                    before = at;
                }

                var steps = Math.Max(_finished.Count - 1, 0);
                var delay = (double)options.RequestDelayMs;
                var mean = (sum + PriorWeight * delay) / (steps + PriorWeight);

                return Math.Round(Math.Max(mean, delay) / 1000, 2);
            }
        }
    }

    public void Finished()
    {
        Finished(Environment.TickCount64);
    }

    public void Finished(long at)
    {
        lock (_gate)
        {
            _finished.Enqueue(at);

            while (at - _finished.Peek() > Window) _finished.Dequeue();
        }
    }

    public void Idle()
    {
        lock (_gate) _finished.Clear();
    }
}