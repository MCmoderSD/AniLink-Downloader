using AniLink.Api.Helpers;

namespace AniLink.Api.Data.Downloads;

public sealed class ArchivePart(MediaItem item, int index, ArchiveLink link)
{
    private Lock Gate => item.Job.Gate;

    public MediaItem Item => item;

    public int Index => index;

    public ArchiveLink Link => link;

    public PartState State
    {
        get { lock (Gate) return field; }
        private set;
    } = PartState.Pending;

    public DebridLinkFile? Debrid
    {
        get { lock (Gate) return field; }
        private set;
    }

    public long BytesDone
    {
        get { lock (Gate) return field; }
        private set;
    }

    public long BytesTotal
    {
        get { lock (Gate) return field; }
        private set;
    }

    public int Attempts
    {
        get { lock (Gate) return field; }
        private set;
    }

    public string? Error
    {
        get { lock (Gate) return field; }
        private set;
    }

    public bool HasLink => Debrid is not null;

    public string StorageName
    {
        get
        {
            var name = BaseName;
            return item.Parts.Take(index).Any(earlier => string.Equals(earlier.BaseName, name, StringComparison.OrdinalIgnoreCase))
                ? $"{index + 1:D3}-{name}"
                : name;
        }
    }

    private string BaseName
    {
        get
        {
            var name = FileNames.Sanitize(link.LooksLikeArchive ? link.FileName : Debrid?.Name);
            if (name.Length == 0) name = FileNames.Sanitize(Debrid?.Name);
            if (name.Length == 0) name = FileNames.Sanitize(link.FileName);

            return name.Length == 0 ? $"part{index + 1:D3}" : name;
        }
    }

    public void BeginResolving()
    {
        lock (Gate)
        {
            State = PartState.Resolving;
            Error = null;
            Touch();
        }
    }

    public void Resolved(DebridLinkFile file)
    {
        lock (Gate)
        {
            Debrid = file;
            BytesTotal = file.Size;
            State = PartState.Resolved;
            Error = null;
            Touch();
        }
    }

    public void InvalidateLink()
    {
        lock (Gate)
        {
            Debrid = null;
            BytesDone = 0;
            BytesTotal = 0;
            State = PartState.Pending;
            Touch();
        }
    }

    public void BeginDownload(long total, long resumedFrom)
    {
        lock (Gate)
        {
            State = PartState.Downloading;
            BytesTotal = total;
            BytesDone = resumedFrom;
            Attempts++;
            Error = null;
            Touch();
        }
    }

    public void Report(long done)
    {
        lock (Gate)
        {
            BytesDone = done;
            Touch();
        }
    }

    public void Downloaded(long size)
    {
        lock (Gate)
        {
            BytesDone = size;
            BytesTotal = size;
            State = PartState.Downloaded;
            Error = null;
            Touch();
        }
    }

    public void Fail(string error)
    {
        lock (Gate)
        {
            State = PartState.Failed;
            Error = error;
            Touch();
        }
    }

    public void Pause()
    {
        lock (Gate)
        {
            if (State is not (PartState.Resolving or PartState.Downloading)) return;

            State = Debrid is null ? PartState.Pending : PartState.Resolved;
            Touch();
        }
    }

    public void ForgetDownload()
    {
        lock (Gate)
        {
            State = Debrid is null ? PartState.Pending : PartState.Resolved;
            BytesDone = 0;
            Touch();
        }
    }

    public void ResetForRetry()
    {
        lock (Gate)
        {
            if (State is PartState.Downloaded) return;

            State = Debrid is null ? PartState.Pending : PartState.Resolved;
            Error = null;
            Attempts = 0;
            BytesDone = 0;
            Touch();
        }
    }

    public void Restore(PartManifest manifest)
    {
        lock (Gate)
        {
            Debrid = manifest.Debrid;
            BytesTotal = manifest.BytesTotal;
            Attempts = manifest.Attempts;
            Error = manifest.Error;

            State = manifest.State switch
            {
                PartState.Downloaded => PartState.Downloaded,
                PartState.Failed => PartState.Failed,
                _ => Debrid is null ? PartState.Pending : PartState.Resolved
            };

            BytesDone = State is PartState.Downloaded ? BytesTotal : 0;
            Touch();
        }
    }

    private void Touch()
    {
        item.Job.Touch();
    }
}