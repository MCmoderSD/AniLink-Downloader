using System.Reflection;

namespace AniLink.Api.Helpers;

public static class AppVersion
{
    // The version from the project file. The SDK appends the commit after a plus sign when it builds inside a
    // repository, and that part says nothing about which release this is.
    public static string Current { get; } = Read();

    private static string Read()
    {
        var version = typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return string.IsNullOrWhiteSpace(version) ? "unknown" : version.Split('+')[0];
    }
}