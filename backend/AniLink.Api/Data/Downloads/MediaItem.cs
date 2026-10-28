namespace AniLink.Api.Data.Downloads;

public sealed class MediaItem
{
    private readonly List<ArchivePart> _parts = [];

    public MediaItem(DownloadJob job, int number, string key, string targetName, IEnumerable<ArchiveLink> links)
    {
        Job = job;
        Number = number;
        Key = key;
        TargetName = targetName;

        var index = 0;
        foreach (var link in links) _parts.Add(new ArchivePart(this, index++, link));
    }

    private Lock Gate => Job.Gate;

    public DownloadJob Job { get; }

    public int Number { get; }

    public string Key { get; }

    public string TargetName { get; }

    public IReadOnlyList<ArchivePart> Parts => _parts;

    public string Label => Job.Release.Kind is ReleaseKind.Movie ? Job.Name : $"Episode {Number}";

    public ItemStage Stage
    {
        get { lock (Gate) return field; }
        private set;
    } = ItemStage.ResolvingLinks;

    public string? Error
    {
        get { lock (Gate) return field; }
        private set;
    }

    public double ExtractPercent
    {
        get { lock (Gate) return field; }
        private set;
    }

    public IReadOnlyList<string> Files
    {
        get { lock (Gate) return field; }
        private set;
    } = [];

    public bool IsActive => Stage is ItemStage.ResolvingLinks or ItemStage.Downloading or ItemStage.Extracting or ItemStage.Organizing;

    public bool IsTerminal => Stage is ItemStage.Completed or ItemStage.Failed or ItemStage.Cancelled;

    public int LinksResolved => _parts.Count(part => part.HasLink);

    public bool AllLinksResolved => _parts.All(part => part.HasLink);

    public long BytesDone => _parts.Sum(part => part.BytesDone);

    public void BeginResolving()
    {
        Move(ItemStage.ResolvingLinks);
    }

    public void BeginDownloading()
    {
        Move(ItemStage.Downloading);
    }

    public bool BeginExtracting()
    {
        lock (Gate)
        {
            if (Stage is ItemStage.Cancelled or ItemStage.Paused) return false;

            Stage = ItemStage.Extracting;
            ExtractPercent = 0;
            Error = null;
            Job.Touch();

            return true;
        }
    }

    public void ReportExtraction(double percent)
    {
        lock (Gate)
        {
            ExtractPercent = percent;
            Job.Touch();
        }
    }

    public bool RequirePassword()
    {
        return Move(ItemStage.PasswordRequired);
    }

    public bool BeginOrganizing()
    {
        return Move(ItemStage.Organizing);
    }

    public void Complete(IReadOnlyList<string> files)
    {
        lock (Gate)
        {
            if (Stage is ItemStage.Cancelled or ItemStage.Paused) return;

            Files = files;
            ExtractPercent = 100;
            Error = null;
            Stage = ItemStage.Completed;
            Job.Touch();
        }
    }

    public void Fail(string error)
    {
        lock (Gate)
        {
            if (Stage is ItemStage.Completed or ItemStage.Cancelled or ItemStage.Paused) return;

            Error = error;
            Stage = ItemStage.Failed;
            Job.Touch();
        }
    }

    public void Cancel()
    {
        lock (Gate)
        {
            if (IsTerminal) return;

            Stage = ItemStage.Cancelled;
            Job.Touch();
        }
    }

    public bool Pause()
    {
        lock (Gate)
        {
            if (Stage is not (ItemStage.ResolvingLinks or ItemStage.Downloading or ItemStage.Extracting)) return false;

            foreach (var part in _parts) part.Pause();

            Stage = ItemStage.Paused;
            ExtractPercent = 0;
            Job.Touch();

            return true;
        }
    }

    public bool Resume()
    {
        lock (Gate)
        {
            if (Stage is not ItemStage.Paused) return false;

            Stage = AllLinksResolved ? ItemStage.Downloading : ItemStage.ResolvingLinks;
            Error = null;
            Job.Touch();

            return true;
        }
    }

    public bool ResetForRetry()
    {
        lock (Gate)
        {
            if (Stage is not (ItemStage.Failed or ItemStage.Cancelled)) return false;

            foreach (var part in _parts) part.ResetForRetry();

            Stage = AllLinksResolved ? ItemStage.Downloading : ItemStage.ResolvingLinks;
            Error = null;
            ExtractPercent = 0;
            Job.Touch();

            return true;
        }
    }

    public void Restore(ItemManifest manifest)
    {
        lock (Gate)
        {
            foreach (var part in manifest.Parts)
            {
                if (part.Index >= 0 && part.Index < _parts.Count) _parts[part.Index].Restore(part);
            }

            Error = manifest.Error;
            Files = manifest.Files;

            Stage = manifest.Stage switch
            {
                ItemStage.Extracting or ItemStage.Organizing or ItemStage.Downloading => AllLinksResolved ? ItemStage.Downloading : ItemStage.ResolvingLinks,
                _ => manifest.Stage
            };

            ExtractPercent = Stage is ItemStage.Completed ? 100 : 0;
            Job.Touch();
        }
    }

    private bool Move(ItemStage stage)
    {
        lock (Gate)
        {
            if (Stage is ItemStage.Cancelled or ItemStage.Paused) return false;

            Stage = stage;
            Error = null;
            Job.Touch();

            return true;
        }
    }
}