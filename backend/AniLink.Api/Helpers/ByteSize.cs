using System.Globalization;

namespace AniLink.Api.Helpers;

public static class ByteSize
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    // Written like the web page writes sizes, so a number in the log and one on the page can be compared
    public static string Format(long bytes)
    {
        if (bytes <= 0) return "0 B";

        var exponent = Math.Min((int)Math.Log(bytes, 1024), Units.Length - 1);
        var value = bytes / Math.Pow(1024, exponent);

        return $"{value.ToString(value >= 100 || exponent == 0 ? "0" : "0.0", CultureInfo.InvariantCulture)} {Units[exponent]}";
    }
}