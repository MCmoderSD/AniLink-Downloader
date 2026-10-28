using System.Globalization;

namespace AniLink.Api.Archives;

internal static class SevenZipListing
{
    public static IReadOnlyList<ArchiveEntry> Parse(string output)
    {
        var entries = new List<ArchiveEntry>();
        var block = new Dictionary<string, string>(StringComparer.Ordinal);
        var inEntries = false;

        foreach (var raw in output.Split('\n'))
        {
            var line = raw.TrimEnd('\r');

            if (!inEntries)
            {
                inEntries = line.StartsWith("----------", StringComparison.Ordinal);
                continue;
            }

            if (line.Length == 0)
            {
                Flush(block, entries);
                continue;
            }

            var separator = line.IndexOf(" = ", StringComparison.Ordinal);
            if (separator > 0) block[line[..separator]] = line[(separator + 3)..];
        }

        Flush(block, entries);

        return entries;
    }

    private static void Flush(Dictionary<string, string> block, List<ArchiveEntry> entries)
    {
        if (block.Count == 0) return;

        if (block.TryGetValue("Path", out var path))
        {
            var size = block.TryGetValue("Size", out var text) && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

            entries.Add(new ArchiveEntry(
                path,
                size,
                block.GetValueOrDefault("Encrypted") == "+",
                block.GetValueOrDefault("Folder") == "+"));
        }

        block.Clear();
    }
}