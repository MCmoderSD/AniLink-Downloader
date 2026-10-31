namespace AniLink.Api.Helpers;

public static class DiskSpace
{
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

    private static bool IsInside(string path, string root, StringComparison comparison)
    {
        if (!path.StartsWith(root, comparison)) return false;

        return path.Length == root.Length || root.EndsWith(Path.DirectorySeparatorChar) || path[root.Length] == Path.DirectorySeparatorChar;
    }
}