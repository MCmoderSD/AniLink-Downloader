using AniLink.Api.Helpers;

namespace AniLink.Api.Data.Downloads;

// Every path a job touches, derived from one folder. Season jobs live in <name>/<season>, a film directly in <name>.
public sealed class Workspace(string root)
{
    public const string ManifestName = ".anilink.json";

    public string Root => root;

    public string ArchivesRoot => Path.Combine(root, "archives");

    public string OutputRoot => Path.Combine(root, "output");

    public string LinkList => Path.Combine(root, "Downloads.txt");

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

    // Archives and unpacked leftovers are only worth deleting when nothing is lost with them. A file still
    // in output/ is something the organizer did not recognise, and the archives are the way to unpack it again.
    public bool CleanUp()
    {
        if (Directory.Exists(OutputRoot) && Directory.EnumerateFiles(OutputRoot, "*", SearchOption.AllDirectories).Any()) return false;

        if (Directory.Exists(OutputRoot)) Directory.Delete(OutputRoot, true);
        if (Directory.Exists(ArchivesRoot)) Directory.Delete(ArchivesRoot, true);

        return true;
    }
}
