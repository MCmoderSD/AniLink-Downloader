namespace AniLink.Api.Helpers;

public static class DiskSpace
{
    public static long? Available(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);

            var drive = DriveInfo.GetDrives()
                .Where(candidate => PathScope.IsInside(full, candidate.RootDirectory.FullName))
                .OrderByDescending(candidate => candidate.RootDirectory.FullName.Length)
                .FirstOrDefault();

            return drive is { IsReady: true } ? drive.AvailableFreeSpace : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }
}