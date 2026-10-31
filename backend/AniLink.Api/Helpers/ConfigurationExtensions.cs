using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace AniLink.Api.Helpers;

public static partial class ConfigurationExtensions
{
    private static readonly Dictionary<string, string> ShortNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Debrid_ApiKey"] = "DebridLink:ApiKey",
        ["Debrid_Request_Delay"] = "DebridLink:RequestDelayMs",
        ["Concurrent_Downloads"] = "Downloads:MaxConcurrency"
    };

    private static readonly Regex PasswordName = MyRegex();

    extension(ConfigurationManager configuration)
    {
        public void UseShortSettingNames()
        {
            var mapped = new Dictionary<string, string?>();

            foreach (var (name, key) in ShortNames)
            {
                var value = configuration[name];
                if (!string.IsNullOrWhiteSpace(value)) mapped[key] = value;
            }

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
                "In development, it comes from appsettings.Local.json or user secrets " +
                $"(UserSecretsId {secretsId ?? "<none>"}; check with: dotnet user-secrets list).");
        }
    }

    [GeneratedRegex("^Archive_Password_([0-9]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}