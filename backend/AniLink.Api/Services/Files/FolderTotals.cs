using System.Collections.Concurrent;
using System.IO.Enumeration;
using AniLink.Api.Helpers;

namespace AniLink.Api.Services.Files;

public sealed class FolderTotals
{
    private const int MaxDepth = 128;

    private static readonly long Lifetime = (long)TimeSpan.FromSeconds(30).TotalMilliseconds;

    private static readonly EnumerationOptions Options = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint
    };

    private readonly ConcurrentDictionary<string, Totals> _cache = new(PathScope.Comparer);
    private long _generation;

    public readonly record struct Totals(long Bytes, int Files, long Stamp);

    public Totals Measure(string directory)
    {
        return Measure(directory, 0);
    }

    public void Invalidate(string path)
    {
        Interlocked.Increment(ref _generation);

        foreach (var key in _cache.Keys)
        {
            if (PathScope.IsInside(key, path) || PathScope.IsInside(path, key)) _cache.TryRemove(key, out _);
        }
    }

    public void Clear()
    {
        Interlocked.Increment(ref _generation);
        _cache.Clear();
    }

    private Totals Measure(string directory, int depth)
    {
        if (_cache.TryGetValue(directory, out var cached) && Environment.TickCount64 - cached.Stamp < Lifetime) return cached;

        var generation = Interlocked.Read(ref _generation);

        long bytes = 0;
        var files = 0;

        try
        {
            var children = new FileSystemEnumerable<(string Name, bool IsDirectory, long Length)>(directory, static (ref entry) => (entry.FileName.ToString(), entry.IsDirectory, entry.Length), Options)
            {
                ShouldIncludePredicate = static (ref entry) => !FilePaths.IsInternal(entry.FileName.ToString())
            };

            foreach (var (name, isDirectory, length) in children)
            {
                if (!isDirectory)
                {
                    bytes += length;
                    files++;
                }
                else if (depth < MaxDepth)
                {
                    var inner = Measure(Path.Join(directory, name), depth + 1);

                    bytes += inner.Bytes;
                    files += inner.Files;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new Totals(bytes, files, Environment.TickCount64);
        }

        var totals = new Totals(bytes, files, Environment.TickCount64);

        if (Interlocked.Read(ref _generation) == generation) _cache[directory] = totals;

        return totals;
    }
}