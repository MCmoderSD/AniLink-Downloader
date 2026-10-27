using AniLink.Api.Helpers;

namespace AniLink.Api.Data.Downloads;

// One file of a (possibly multipart) archive. All state changes go through the methods below, which take the
// lock of the owning job, so the pipeline, the downloads and the progress publisher can all touch a part at once.
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

    // The name on disk. 7-Zip finds the later volumes of an archive by their names, so the name from the pasted
    // URL (which carries .part2.rar or .r01) is kept whenever it looks like an archive; hosts that hide it in
    // the URL fall back to the name Debrid-Link reports. Two parts that would end up with the same name are
    // told apart by their position, which keeps both files at the price of 7-Zip not finding the second.
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

    // A download link that Debrid-Link no longer honours: the part goes back to needing a link.
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

    // What was in flight stops here. The part keeps the bytes it has, and so does its file on disk, and goes back to
    // waiting for its turn: Resume continues the download from that file.
    public void Pause()
    {
        lock (Gate)
        {
            if (State is not (PartState.Resolving or PartState.Downloading)) return;

            State = Debrid is null ? PartState.Pending : PartState.Resolved;
            Touch();
        }
    }

    // The file this part was recorded as downloaded is gone or has the wrong size: it has to be fetched again.
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

            // Whatever was in flight when the container went down starts over (a partial file is resumed by
            // the downloader); only a finished download and a recorded failure are taken at face value.
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
