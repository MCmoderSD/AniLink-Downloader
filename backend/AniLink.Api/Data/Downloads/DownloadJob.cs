using AniLink.Api.Helpers;

namespace AniLink.Api.Data.Downloads;

public sealed record Release(ReleaseKind Kind, int? Season, string? Format);

public sealed class DownloadJob(Guid id, string name, DateTimeOffset createdAt, Release release, Workspace workspace, PasswordBook passwords)
{
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
            if (stages.Any(stage => stage is ItemStage.Downloaded)) return JobStatus.Downloaded;

            return JobStatus.Completed;
        }
    }

    public IEnumerable<ArchivePart> Parts => _items.SelectMany(item => item.Parts);

    public int LinksTotal => Parts.Count();

    public int LinksResolved => Parts.Count(part => part.HasLink);

    public long BytesDone => Parts.Sum(part => part.BytesDone);

    public long BytesTotal => EstimateTotal([.. Parts]);

    public static long EstimateTotal(IReadOnlyList<ArchivePart> parts)
    {
        var known = parts.Select(part => part.BytesTotal).Where(total => total > 0).ToList();
        if (known.Count == 0) return 0;

        return known.Sum() + known.Sum() / known.Count * (parts.Count - known.Count);
    }

    public MediaItem AddItem(int number, IEnumerable<ArchiveLink> links, bool unpack = true)
    {
        var movie = release.Kind is ReleaseKind.Movie;

        var item = new MediaItem(
            this,
            number,
            movie ? "" : number.ToString(),
            movie ? FileNames.Sanitize(name) : number.ToString(),
            links,
            unpack);

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
            job.AddItem(item.Number, links, item.Unpack).Restore(item);
        }

        job.Activity.Restore(manifest.Activity);

        return job;
    }
}