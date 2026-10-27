using AniLink.Api.Data.Downloads;
using AniLink.Api.Exceptions.Downloads;

namespace AniLink.Api.Services;

// Takes what an archive unpacked to and turns it into the file the media server will see: exactly one video,
// renamed after the episode or film, plus its .nfo, moved up into the workdir. Anything else stays in output/.
public sealed class MediaOrganizer(ILogger<MediaOrganizer> logger)
{
    private static readonly string[] VideoExtensions = [".mkv", ".mp4"];

    public IReadOnlyList<string> Organize(Workspace workspace, MediaItem item)
    {
        var output = workspace.OutputDirectory(item);

        var files = Directory.Exists(output)
            ? Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories).ToList()
            : [];

        var videos = files.Where(file => VideoExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)).ToList();

        if (videos.Count == 0) throw new MediaOrganizationException("The archive contains no .mkv or .mp4 file.");
        if (videos.Count > 1) throw new MediaOrganizationException($"The archive contains {videos.Count} video files, but exactly one was expected.");

        var video = videos[0];
        var notes = files.Where(file => string.Equals(Path.GetExtension(file), ".nfo", StringComparison.OrdinalIgnoreCase)).ToList();

        // The .nfo that belongs to the video, or the only one there is. Several unrelated ones are not guessed at.
        var note = notes.FirstOrDefault(file => string.Equals(Path.GetFileNameWithoutExtension(file), Path.GetFileNameWithoutExtension(video), StringComparison.OrdinalIgnoreCase))
                   ?? (notes.Count == 1 ? notes[0] : null);

        var moves = new List<(string From, string To)> { (video, workspace.TargetPath(item, Path.GetExtension(video).ToLowerInvariant())) };
        if (note is not null) moves.Add((note, workspace.TargetPath(item, ".nfo")));

        // Checked up front so a clash does not leave the video moved and its .nfo behind.
        foreach (var (_, target) in moves)
        {
            if (File.Exists(target)) throw new MediaOrganizationException($"{Path.GetFileName(target)} already exists in {workspace.Root}.");
        }

        foreach (var (from, to) in moves) File.Move(from, to);

        logger.LogInformation("Organized {Item} of {Job} into {Files}", item.Label, item.Job.Name, string.Join(", ", moves.Select(move => Path.GetFileName(move.To))));

        return [.. moves.Select(move => Path.GetFileName(move.To))];
    }
}
