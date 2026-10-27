using System.Reflection;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace AniLink.Api.Helpers;

public static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public string GetRequiredValue(string key, string? context = null)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value)) return value;

            var secretsId = Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;
            var environmentVariable = key.Replace(":", "__", StringComparison.Ordinal);

            throw new InvalidOperationException(
                $"Missing configuration '{key}'" + (context is null ? "" : $" ({context})") + ". " +
                $"In Docker it arrives as the environment variable {environmentVariable} (docker-compose.yaml, filled from .env). " +
                "In development it comes from appsettings.Local.json or user secrets " +
                $"(UserSecretsId {secretsId ?? "<none>"}; check with: dotnet user-secrets list).");
        }
    }
}
