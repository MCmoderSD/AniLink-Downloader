namespace AniLink.Api.Data.Downloads;

public sealed class ExtractPace
{
    private const long Window = 30_000;
    private const long ShortestSpan = 3_000;

    private readonly Lock _gate = new();
    private readonly List<(long At, double Percent)> _steps = [];

    public void Reset()
    {
        lock (_gate) _steps.Clear();
    }

    public void Report(double percent)
    {
        Report(percent, Environment.TickCount64);
    }

    private void Report(double percent, long at)
    {
        lock (_gate)
        {
            if (_steps.Count > 0 && percent < _steps[^1].Percent) _steps.Clear();
            if (_steps.Count == 0 || percent > _steps[^1].Percent) _steps.Add((at, percent));
        }
    }

    public int SecondsLeft()
    {
        return SecondsLeft(Environment.TickCount64);
    }

    private int SecondsLeft(long now)
    {
        lock (_gate)
        {
            if (_steps.Count < 2) return 0;

            var (lastAt, last) = _steps[^1];

            if (last >= 100) return 0;

            var recent = _steps.Where(step => lastAt - step.At <= Window).ToList();
            var first = recent.Count >= 3 && lastAt - recent[0].At >= ShortestSpan ? recent[0] : _steps[0];
            var span = (lastAt - first.At) / 1000d;

            if (span < ShortestSpan / 1000d || last <= first.Percent) return 0;

            var rate = (last - first.Percent) / span;
            var done = Math.Min(last + Math.Max(now - lastAt, 0) / 1000d * rate, Math.Min(last + 1, 100));

            return Math.Max((int)Math.Ceiling((100 - done) / rate), 1);
        }
    }
}