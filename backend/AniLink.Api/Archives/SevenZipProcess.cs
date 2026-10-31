using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AniLink.Api.Exceptions.Archives;

namespace AniLink.Api.Archives;

public sealed record ToolResult(int ExitCode, string Output, string Error)
{
    public string Combined => (Output + "\n" + Error).Trim();
}

public sealed class ToolActivity
{
    private const int TailLength = 2048;

    private readonly Lock _gate = new();
    private long _chunks;
    private long _characters;
    private string _first = "";
    private string _tail = "";
    private double _percent;
    private Process? _process;

    public long Chunks
    {
        get { lock (_gate) return _chunks; }
    }

    public long Characters
    {
        get { lock (_gate) return _characters; }
    }

    public string First
    {
        get { lock (_gate) return _first; }
    }

    public string Tail
    {
        get { lock (_gate) return _tail; }
    }

    public double Percent
    {
        get { lock (_gate) return _percent; }
    }

    public void Record(string chunk)
    {
        lock (_gate)
        {
            _chunks++;
            _characters += chunk.Length;

            if (_first.Length == 0) _first = chunk;

            _tail = (_tail + chunk)[^Math.Min(_tail.Length + chunk.Length, TailLength)..];
        }
    }

    public void Reported(double percent)
    {
        lock (_gate) _percent = percent;
    }

    public void Attach(Process process)
    {
        lock (_gate) _process = process;
    }

    public TimeSpan? CpuTime
    {
        get
        {
            Process? process;

            lock (_gate) process = _process;

            try
            {
                return process?.TotalProcessorTime;
            }
            catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
            {
                return null;
            }
        }
    }
}

public sealed partial class SevenZipProcess(ArchiveOptions options)
{
    private const int WindowLength = 1024;

    [GeneratedRegex(@"^\s*(\d{1,3})%")]
    private static partial Regex ProgressPattern();

    public string Executable => options.SevenZipPath;

    public async Task<ToolResult> RunAsync(IEnumerable<string> arguments, Action<double>? onProgress, CancellationToken cancellationToken, ToolActivity? activity = null)
    {
        using var process = Start(arguments);

        activity?.Attach(process);

        var output = new StringBuilder();
        var error = process.StandardError.ReadToEndAsync(cancellationToken);

        await using var registration = cancellationToken.Register(static state => Kill((Process)state!), process);

        var buffer = new char[4096];
        var window = "";
        int read;

        while ((read = await process.StandardOutput.ReadAsync(buffer, cancellationToken)) > 0)
        {
            var chunk = new string(buffer, 0, read);

            activity?.Record(chunk);

            if (output.Length < 1_000_000) output.Append(chunk);

            if (onProgress is null) continue;

            var cut = window.Length + chunk.Length > WindowLength;
            window = (window + chunk)[^Math.Min(window.Length + chunk.Length, WindowLength)..];

            if (LatestPercent(window, cut) is not { } percent) continue;

            activity?.Reported(percent);
            onProgress(percent);
        }

        await process.WaitForExitAsync(cancellationToken);

        return new ToolResult(process.ExitCode, output.ToString(), await error);
    }

    private static double? LatestPercent(string window, bool cut)
    {
        var segments = window.Split(['\b', '\r', '\n']);

        for (var index = segments.Length - 1; index >= (cut ? 1 : 0); index--)
        {
            var match = ProgressPattern().Match(segments[index]);
            if (match.Success && double.TryParse(match.Groups[1].ValueSpan, out var percent)) return Math.Min(percent, 100);
        }

        return null;
    }

    public async Task<byte[]?> ReadHeadAsync(IEnumerable<string> arguments, int count, CancellationToken cancellationToken)
    {
        using var process = Start(arguments);

        _ = process.StandardError.ReadToEndAsync(cancellationToken);

        await using var registration = cancellationToken.Register(static state => Kill((Process)state!), process);

        var buffer = new byte[count];
        var total = 0;

        try
        {
            int read;
            while (total < count && (read = await process.StandardOutput.BaseStream.ReadAsync(buffer.AsMemory(total), cancellationToken)) > 0) total += read;
        }
        finally
        {
            Kill(process);
        }

        return total == count ? buffer : null;
    }

    private Process Start(IEnumerable<string> arguments)
    {
        var info = new ProcessStartInfo(options.SevenZipPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            Environment =
            {
                ["LC_ALL"] = "C.UTF-8"
            }
        };

        foreach (var argument in arguments) info.ArgumentList.Add(argument);

        Process process;

        try
        {
            process = Process.Start(info) ?? throw new ExtractionException("7-Zip could not be started.");
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            throw new ExtractionException($"7-Zip was not found at '{options.SevenZipPath}'. Set Archive__SevenZipPath to the 7zz or 7z.exe binary.", exception);
        }

        process.StandardInput.Close();

        return process;
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(true);
        }
        catch (InvalidOperationException)
        {
            // It ended between the check and the kill.
        }
    }
}