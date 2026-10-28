using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AniLink.Api.Exceptions.Archives;

namespace AniLink.Api.Archives;

public sealed record ToolResult(int ExitCode, string Output, string Error)
{
    public string Combined => (Output + "\n" + Error).Trim();
}

public sealed partial class SevenZipProcess(ArchiveOptions options)
{
    [GeneratedRegex(@"(\d{1,3})%")]
    private static partial Regex ProgressPattern();

    public string Executable => options.SevenZipPath;

    public async Task<ToolResult> RunAsync(IEnumerable<string> arguments, Action<double>? onProgress, CancellationToken cancellationToken)
    {
        using var process = Start(arguments);

        var output = new StringBuilder();
        var error = process.StandardError.ReadToEndAsync(cancellationToken);

        await using var registration = cancellationToken.Register(static state => Kill((Process)state!), process);

        var buffer = new char[4096];
        var window = "";
        int read;

        while ((read = await process.StandardOutput.ReadAsync(buffer, cancellationToken)) > 0)
        {
            var chunk = new string(buffer, 0, read);

            if (output.Length < 1_000_000) output.Append(chunk);

            if (onProgress is null) continue;

            window = (window + chunk)[^Math.Min(window.Length + chunk.Length, 64)..];
            var matches = ProgressPattern().Matches(window);
            if (matches.Count > 0 && double.TryParse(matches[^1].Groups[1].ValueSpan, out var percent)) onProgress(Math.Min(percent, 100));
        }

        await process.WaitForExitAsync(cancellationToken);

        return new ToolResult(process.ExitCode, output.ToString(), await error);
    }

    // Reads the first bytes of one entry, decrypted, and stops the tool there. That is enough to tell a good
    // password from a bad one without unpacking a multi-gigabyte episode for every candidate.
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