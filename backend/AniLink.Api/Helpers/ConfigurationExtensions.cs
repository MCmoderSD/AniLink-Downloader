using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace AniLink.Api.Helpers;

public static class ConfigurationExtensions
{
    // What the container is configured with, and the setting each name stands for.
    private static readonly Dictionary<string, string> ShortNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Debrid_ApiKey"] = "DebridLink:ApiKey",
        ["Debrid_Request_Delay"] = "DebridLink:RequestDelayMs",
        ["Concurrent_Downloads"] = "Downloads:MaxConcurrency"
    };

    private static readonly Regex PasswordName = new("^Archive_Password_([0-9]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    extension(ConfigurationManager configuration)
    {
        // The environment knows Debrid_ApiKey, Debrid_Request_Delay, Concurrent_Downloads and Archive_Password_0, _1, ...; the code
        // knows DebridLink:ApiKey, Archive:Passwords:0 and so on. This copies the first onto the second, after every
        // other source, so a short name wins. The structured names keep working: they are simply not touched.
        public void UseShortSettingNames()
        {
            var mapped = new Dictionary<string, string?>();

            foreach (var (name, key) in ShortNames)
            {
                var value = configuration[name];
                if (!string.IsNullOrWhiteSpace(value)) mapped[key] = value;
            }

            // Any number of passwords, in the order of their numbers; a gap or an empty one is simply skipped.
            var passwords = configuration.AsEnumerable()
                .Select(pair => (Match: PasswordName.Match(pair.Key), pair.Value))
                .Where(entry => entry.Match.Success && !string.IsNullOrWhiteSpace(entry.Value))
                .OrderBy(entry => entry.Match.Groups[1].Value.TrimStart('0').Length)
                .ThenBy(entry => entry.Match.Groups[1].Value.TrimStart('0'), StringComparer.Ordinal)
                .Select(entry => entry.Value!)
                .ToList();

            for (var index = 0; index < passwords.Count; index++) mapped[$"Archive:Passwords:{index}"] = passwords[index];

            if (mapped.Count > 0) configuration.AddInMemoryCollection(mapped);
        }
    }

    extension(IConfiguration configuration)
    {
        public string GetRequiredValue(string key, string? context = null, string? environmentVariable = null)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value)) return value;

            var secretsId = Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;
            environmentVariable ??= key.Replace(":", "__", StringComparison.Ordinal);

            throw new InvalidOperationException(
                $"Missing configuration '{key}'" + (context is null ? "" : $" ({context})") + ". " +
                $"In Docker it arrives as the environment variable {environmentVariable} (docker-compose.yaml, filled from .env). " +
                "In development it comes from appsettings.Local.json or user secrets " +
                $"(UserSecretsId {secretsId ?? "<none>"}; check with: dotnet user-secrets list).");
        }
    }
}