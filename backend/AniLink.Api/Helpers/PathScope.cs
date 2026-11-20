namespace AniLink.Api.Helpers;

public static class PathScope
{
    public static StringComparison Comparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static StringComparer Comparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static bool IsInside(string path, string root)
    {
        return IsInside(path, root, Comparison);
    }

    public static bool IsInside(string path, string root, StringComparison comparison)
    {
        if (!path.StartsWith(root, comparison)) return false;

        return path.Length == root.Length || root.EndsWith(Path.DirectorySeparatorChar) || path[root.Length] == Path.DirectorySeparatorChar;
    }

    public static bool Overlaps(string first, string second, StringComparison comparison)
    {
        return IsInside(first, second, comparison) || IsInside(second, first, comparison);
    }
}