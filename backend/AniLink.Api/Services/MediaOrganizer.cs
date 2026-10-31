using AniLink.Api.Data.Downloads;
using AniLink.Api.Exceptions.Downloads;

namespace AniLink.Api.Services;

public sealed class MediaOrganizer(ILogger<MediaOrganizer> logger)
{
    private const string InfoName = "info.txt";

    private static readonly string[] VideoExtensions = [".mkv", ".mp4"];

    private static readonly string[] IgnoredExtensions = [".md5", ".download", ".url", ".uri"];

    public IReadOnlyList<string> Organize(Workspace workspace, MediaItem item)
    {
        var output = workspace.OutputDirectory(item);

        var files = Directory.Exists(output)
            ? Directory.EnumerateFiles(output, "*", Workspace.FilesWithoutLinks).ToList()
            : [];

        var videos = files.Where(file => VideoExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)).ToList();

        if (videos.Count == 0) throw new MediaOrganizationException("The archive contains no .mkv or .mp4 file.");
        if (videos.Count > 1) throw new MediaOrganizationException($"The archive contains {videos.Count} video files, but exactly one was expected.");

        var video = videos[0];
        var notes = files.Where(file => string.Equals(Path.GetExtension(file), ".nfo", StringComparison.OrdinalIgnoreCase)).ToList();

        var note = notes.FirstOrDefault(file => string.Equals(Path.GetFileNameWithoutExtension(file), Path.GetFileNameWithoutExtension(video), StringComparison.OrdinalIgnoreCase))
                   ?? (notes.Count == 1 ? notes[0] : null);

        var infos = files.Where(file => string.Equals(Path.GetFileName(file), InfoName, StringComparison.OrdinalIgnoreCase)).ToList();
        var info = infos.Count == 1 ? infos[0] : null;
        var infoTarget = workspace.TargetPath(item, ".txt");

        var moves = new List<(string From, string To)> { (video, workspace.TargetPath(item, Path.GetExtension(video).ToLowerInvariant())) };
        if (note is not null) moves.Add((note, workspace.TargetPath(item, ".nfo")));

        if (info is not null)
        {
            if (File.Exists(infoTarget)) logger.LogInformation("The info.txt of {Item} stays in the output, because {Target} exists already", item.LogLabel, Path.GetFileName(infoTarget));
            else moves.Add((info, infoTarget));
        }

        foreach (var (_, target) in moves)
        {
            if (File.Exists(target)) throw new MediaOrganizationException($"{Path.GetFileName(target)} already exists in {workspace.Root}.");
        }

        foreach (var (from, to) in moves) File.Move(from, to);

        DeleteIgnored(files);

        logger.LogInformation("Organized {Item} into {Files}", item.LogLabel, string.Join(", ", moves.Select(move => Path.GetFileName(move.To))));

        return [.. moves.Select(move => Path.GetFileName(move.To))];
    }

    private static void DeleteIgnored(IEnumerable<string> files)
    {
        foreach (var file in files.Where(file => IgnoredExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A file that cannot be deleted stays in output/ with the rest, so nothing is lost and the item is not failed for it
            }
        }
    }
}