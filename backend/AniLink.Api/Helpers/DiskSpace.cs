namespace AniLink.Api.Helpers;

public static class DiskSpace
{
    // The bytes still free on the volume that holds the path, or null when that cannot be told. The volume is the
    // mounted drive whose root is the longest start of the path, which is what a bind mount at /downloads is.
    public static long? Available(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            var drive = DriveInfo.GetDrives()
                .Where(candidate => IsInside(full, candidate.RootDirectory.FullName, comparison))
                .OrderByDescending(candidate => candidate.RootDirectory.FullName.Length)
                .FirstOrDefault();

            return drive is { IsReady: true } ? drive.AvailableFreeSpace : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    // A path is inside a root when it starts with it and the next character starts a new name, so /downloads2 is
    // not inside /downloads.
    private static bool IsInside(string path, string root, StringComparison comparison)
    {
        if (!path.StartsWith(root, comparison)) return false;

        return path.Length == root.Length || root.EndsWith(Path.DirectorySeparatorChar) || path[root.Length] == Path.DirectorySeparatorChar;
    }
}