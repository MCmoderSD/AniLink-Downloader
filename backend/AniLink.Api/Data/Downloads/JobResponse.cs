using JetBrains.Annotations;

namespace AniLink.Api.Data.Downloads;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record ProgressResponse(long Done, long Total, double Fraction)
{
    public static ProgressResponse Of(long done, long total)
    {
        return new ProgressResponse(done, total, total <= 0 ? 0 : Math.Clamp((double)done / total, 0, 1));
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record NoticeResponse(DateTimeOffset At, NoticeLevel Level, string Text)
{
    public static NoticeResponse From(ActivityEntry entry)
    {
        return new NoticeResponse(entry.At, entry.Level, entry.Text);
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record PartResponse(
    int Index,
    string FileName,
    PartState State,
    long BytesDone,
    long BytesTotal,
    int Attempts,
    string? Error,
    string? Host
) {
    public static PartResponse From(ArchivePart part)
    {
        return new PartResponse(
            part.Index,
            part.StorageName,
            part.State,
            part.BytesDone,
            part.BytesTotal,
            part.Attempts,
            part.Error,
            part.Debrid?.Host);
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record ItemResponse(
    int Number,
    string Label,
    ItemStage Stage,
    string? Error,
    ProgressResponse Links,
    ProgressResponse Download,
    double ExtractPercent,
    IReadOnlyList<string> Files,
    IReadOnlyList<PartResponse> Parts,
    bool Unpack,
    bool CanChangeUnpack
) {
    public static ItemResponse From(MediaItem item)
    {
        return new ItemResponse(
            item.Number,
            item.Label,
            item.Stage,
            item.Error,
            ProgressResponse.Of(item.LinksResolved, item.Parts.Count),
            ProgressResponse.Of(item.BytesDone, DownloadJob.EstimateTotal(item.Parts)),
            item.ExtractPercent,
            item.Files,
            [.. item.Parts.Select(PartResponse.From)],
            item.UnpackWanted,
            item.CanChangeUnpack);
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record JobResponse(
    Guid Id,
    string Name,
    ReleaseKind Kind,
    int? Season,
    string? Format,
    JobStatus Status,
    DateTimeOffset CreatedAt,
    string Directory,
    ProgressResponse Links,
    ProgressResponse Download,
    double SpeedBytesPerSecond,
    int ItemsCompleted,
    IReadOnlyList<ItemResponse> Items,
    IReadOnlyList<NoticeResponse> Activity
) {
    private const int ActivityShown = 60;

    public static JobResponse From(DownloadJob job)
    {
        lock (job.Gate)
        {
            return new JobResponse(
                job.Id,
                job.Name,
                job.Release.Kind,
                job.Release.Season,
                job.Release.Format,
                job.Status,
                job.CreatedAt,
                job.Workspace.Root,
                ProgressResponse.Of(job.LinksResolved, job.LinksTotal),
                ProgressResponse.Of(job.BytesDone, job.BytesTotal),
                job.SpeedBytesPerSecond,
                job.Items.Count(item => item.Stage is ItemStage.Completed),
                [.. job.Items.Select(ItemResponse.From)],
                [.. job.Activity.Entries().TakeLast(ActivityShown).Select(NoticeResponse.From)]);
        }
    }
}