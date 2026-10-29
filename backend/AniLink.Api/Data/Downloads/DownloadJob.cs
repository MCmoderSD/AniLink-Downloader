using AniLink.Api.Helpers;

namespace AniLink.Api.Data.Downloads;

public sealed record Release(ReleaseKind Kind, int? Season, string? Format);

public sealed class DownloadJob(Guid id, string name, DateTimeOffset createdAt, Release release, Workspace workspace, PasswordBook passwords)
{
    // Replaced as a whole whenever an item is added or removed, so the many readers that walk it without a lock
    // always see one consistent list.
    private volatile MediaItem[] _items = [];
    private long _revision;
    private int _linksAnnounced;

    internal Lock Gate { get; } = new();

    public Guid Id => id;

    public string Name => name;

    public DateTimeOffset CreatedAt => createdAt;

    public Release Release => release;

    public Workspace Workspace => workspace;

    public PasswordBook Passwords => passwords;

    public ActivityLog Activity { get; } = new();

    public IReadOnlyList<MediaItem> Items => _items;

    // Bumped by every change. The progress publisher compares it with what it sent last, so a job that
    // sits idle costs nothing.
    public long Revision => Interlocked.Read(ref _revision);

    public bool IsRemoved
    {
        get { lock (Gate) return field; }
        private set;
    }

    public double SpeedBytesPerSecond
    {
        get { lock (Gate) return field; }
        set { lock (Gate) field = value; }
    }

    public JobStatus Status
    {
        get
        {
            var stages = _items.Select(item => item.Stage).ToList();

            if (stages.Any(stage => stage is ItemStage.ResolvingLinks or ItemStage.Downloading or ItemStage.Extracting or ItemStage.Organizing)) return JobStatus.Active;
            if (stages.Any(stage => stage is ItemStage.Paused)) return JobStatus.Paused;
            if (stages.Any(stage => stage is ItemStage.PasswordRequired)) return JobStatus.PasswordRequired;
            if (stages.Any(stage => stage is ItemStage.Failed)) return JobStatus.Failed;
            if (stages.Any(stage => stage is ItemStage.Cancelled)) return JobStatus.Cancelled;

            return JobStatus.Completed;
        }
    }

    public IEnumerable<ArchivePart> Parts => _items.SelectMany(item => item.Parts);

    public int LinksTotal => Parts.Count();

    public int LinksResolved => Parts.Count(part => part.HasLink);

    public long BytesDone => Parts.Sum(part => part.BytesDone);

    public long BytesTotal => EstimateTotal(Parts.ToList());

    // Sizes only become known as links are resolved, so parts without one are assumed to be as large as
    // the average of those that have one. The bar then fills evenly instead of jumping on every new link.
    public static long EstimateTotal(IReadOnlyList<ArchivePart> parts)
    {
        var known = parts.Select(part => part.BytesTotal).Where(total => total > 0).ToList();
        if (known.Count == 0) return 0;

        return known.Sum() + known.Sum() / known.Count * (parts.Count - known.Count);
    }

    public MediaItem AddItem(int number, IEnumerable<ArchiveLink> links)
    {
        var movie = release.Kind is ReleaseKind.Movie;

        var item = new MediaItem(
            this,
            number,
            movie ? "movie" : number.ToString("D3"),
            movie ? FileNames.Sanitize(name) : number.ToString(),
            links);

        lock (Gate) _items = [.. _items, item];

        return item;
    }

    public MediaItem? FindItem(int number)
    {
        return _items.FirstOrDefault(item => item.Number == number);
    }

    public void RemoveItem(MediaItem item)
    {
        lock (Gate) _items = [.. _items.Where(candidate => candidate != item)];

        Touch();
    }

    public ActivityEntry Log(NoticeLevel level, string text)
    {
        var entry = Activity.Add(level, text);
        Touch();

        return entry;
    }

    public void MarkRemoved()
    {
        lock (Gate) IsRemoved = true;
    }

    // True exactly once, for the caller that gets to announce that every link of the job exists.
    public bool TryAnnounceLinks()
    {
        return Interlocked.Exchange(ref _linksAnnounced, 1) == 0;
    }

    internal void Touch()
    {
        Interlocked.Increment(ref _revision);
    }

    public static DownloadJob Restore(JobManifest manifest, string directory, IReadOnlyList<string> defaultPasswords)
    {
        var passwords = new PasswordBook(defaultPasswords);
        passwords.Restore(manifest.Passwords, manifest.WorkingPassword);

        var job = new DownloadJob(
            manifest.Id,
            manifest.Name,
            manifest.CreatedAt,
            new Release(manifest.Kind, manifest.Season, manifest.Format),
            new Workspace(directory),
            passwords);

        foreach (var item in manifest.Items)
        {
            var links = item.Parts.OrderBy(part => part.Index).Select(part => new ArchiveLink(part.Url, part.FileName, part.PartNumber));
            job.AddItem(item.Number, links).Restore(item);
        }

        job.Activity.Restore(manifest.Activity);

        return job;
    }
}