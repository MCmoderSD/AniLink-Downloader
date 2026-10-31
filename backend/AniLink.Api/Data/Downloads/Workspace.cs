using AniLink.Api.Helpers;

namespace AniLink.Api.Data.Downloads;

public sealed class Workspace(string root)
{
    public const string ManifestName = ".anilink.json";

    // The two folders that hold what comes out of downloaded archives. Nothing in them belongs to the app itself.
    public const string ArchivesFolder = "archives";
    public const string OutputFolder = "output";

    // Every file below a folder, except links. A link to a folder would show its files a second time, and a link to
    // nothing would still be listed as a file. Hidden files are included, as they are with a plain search.
    public static readonly EnumerationOptions FilesWithoutLinks = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.ReparsePoint
    };

    public string Root => root;

    private string ArchivesRoot => Path.Combine(root, ArchivesFolder);

    private string OutputRoot => Path.Combine(root, OutputFolder);

    private string LinkList => Path.Combine(root, "Downloads.txt");

    public string Manifest => Path.Combine(root, ManifestName);

    public static Workspace For(string downloadsRoot, string name, ReleaseKind kind, int? season)
    {
        var folder = FileNames.Sanitize(name);

        return new Workspace(kind is ReleaseKind.Season
            ? Path.Combine(downloadsRoot, folder, (season ?? 1).ToString("D2"))
            : Path.Combine(downloadsRoot, folder));
    }

    public string ArchiveDirectory(MediaItem item)
    {
        return Path.Combine(ArchivesRoot, item.Key);
    }

    public string OutputDirectory(MediaItem item)
    {
        return Path.Combine(OutputRoot, item.Key);
    }

    public string TargetPath(MediaItem item, string extension)
    {
        return Path.Combine(root, item.TargetName + extension);
    }

    public void Prepare(IEnumerable<MediaItem> items)
    {
        Directory.CreateDirectory(ArchivesRoot);
        Directory.CreateDirectory(OutputRoot);

        File.WriteAllLines(LinkList, items
            .OrderBy(item => item.Number)
            .SelectMany(item => item.Parts)
            .Select(part => part.Link.Url));
    }

    public bool CleanUp()
    {
        if (Directory.Exists(OutputRoot) && Directory.EnumerateFiles(OutputRoot, "*", FilesWithoutLinks).Any()) return false;

        if (Directory.Exists(OutputRoot)) Directory.Delete(OutputRoot, true);
        if (Directory.Exists(ArchivesRoot)) Directory.Delete(ArchivesRoot, true);

        return true;
    }
}