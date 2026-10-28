namespace AniLink.Api.Data.Downloads;

public sealed record ActivityEntry(DateTimeOffset At, NoticeLevel Level, string Text);

public sealed class ActivityLog
{
    private const int Capacity = 200;

    private readonly Lock _gate = new();
    private readonly List<ActivityEntry> _entries = [];

    public ActivityEntry Add(NoticeLevel level, string text)
    {
        var entry = new ActivityEntry(DateTimeOffset.UtcNow, level, text);

        lock (_gate)
        {
            _entries.Add(entry);
            if (_entries.Count > Capacity) _entries.RemoveRange(0, _entries.Count - Capacity);
        }

        return entry;
    }

    public IReadOnlyList<ActivityEntry> Entries()
    {
        lock (_gate) return [.. _entries];
    }

    public void Restore(IEnumerable<ActivityEntry> entries)
    {
        lock (_gate)
        {
            _entries.Clear();
            _entries.AddRange(entries.TakeLast(Capacity));
        }
    }
}