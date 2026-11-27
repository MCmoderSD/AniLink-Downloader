using AniLink.Api.Data.Downloads;
using AniLink.Api.Data.Files;
using AniLink.Api.Helpers;
using AniLink.Api.Repositories;

namespace AniLink.Api.Services.Files;

public sealed class FilePaths(DownloadOptions options)
{
    public string Root { get; } = Path.TrimEndingDirectorySeparator(options.Root);

    public static bool IsInternal(string name)
    {
        return name.Equals(Workspace.ManifestName, PathScope.Comparison)
            || name.Equals(Workspace.ManifestTemporaryName, PathScope.Comparison)
            || name.Equals(StorageHealthCheck.ProbeName, PathScope.Comparison);
    }

    public PathOutcome Resolve(string? relative, bool forDelete)
    {
        relative ??= "";

        if (relative.Length > FileLimits.MaxPathLength) return new PathInvalid("The path is too long.");
        if (relative.Contains('\0')) return new PathInvalid("The path contains a character that is not allowed.");

        if (relative.Length == 0)
        {
            return forDelete ? new PathInvalid("The downloads folder itself cannot be deleted.") : new PathResolved(Root, "");
        }

        var segments = relative.Split('/');

        if (segments.Length > FileLimits.MaxDepth) return new PathInvalid("The path is too deep.");
        if (segments.Any(segment => !IsPlainName(segment))) return new PathInvalid("The path is not a path inside the downloads folder.");
        if (segments.Any(IsInternal)) return new PathMissing();

        var joined = Path.Join(Root, string.Join(Path.DirectorySeparatorChar, segments));
        var full = Path.GetFullPath(joined);

        if (!string.Equals(full, joined, StringComparison.Ordinal) || !PathScope.IsInside(full, Root) || full.Length <= Root.Length)
        {
            return new PathInvalid("The path is not a path inside the downloads folder.");
        }

        var current = Root;

        for (var index = 0; index < segments.Length; index++)
        {
            current = Path.Join(current, segments[index]);

            FileAttributes attributes;

            try
            {
                attributes = File.GetAttributes(current);
            }
            catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
            {
                return new PathMissing();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return new PathInvalid("The path cannot be read.");
            }

            var last = index == segments.Length - 1;

            if (attributes.HasFlag(FileAttributes.ReparsePoint) && (!last || !forDelete))
            {
                return new PathInvalid("The path goes through a link, and links are not followed.");
            }
        }

        return new PathResolved(full, string.Join('/', segments));
    }

    private static bool IsPlainName(string segment)
    {
        if (segment.Length == 0 || segment is "." or "..") return false;

        if (!OperatingSystem.IsWindows()) return true;

        return segment.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !segment.EndsWith('.') && !segment.EndsWith(' ');
    }
}