using System.Text.Json;

namespace ListingAutoPosterSandbox.Web.Services;

public sealed class LocalFacebookTokenStore : ITokenStore
{
    private readonly IWebHostEnvironment _environment;
    private readonly object _lock = new();

    public LocalFacebookTokenStore(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<string> GetAccessTokenAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new InvalidOperationException("SecretName is missing for the selected social account.");
        }

        var tokens = LoadTokens();

        if (tokens.TryGetValue(secretName, out var token) &&
            !string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return Task.FromResult(token.AccessToken);
        }

        throw new InvalidOperationException(
            $"No locally stored Facebook token was found for SecretName '{secretName}'. Connect the Facebook Page first.");
    }

    public Task SaveAccessTokenAsync(
        string secretName,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new InvalidOperationException("SecretName is required before saving a Facebook token.");
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Facebook access token is empty.");
        }

        lock (_lock)
        {
            var tokens = LoadTokens();

            tokens[secretName] = new LocalFacebookToken
            {
                AccessToken = accessToken,
                SavedUtc = DateTime.UtcNow
            };

            SaveTokens(tokens);
        }

        return Task.CompletedTask;
    }

    private Dictionary<string, LocalFacebookToken> LoadTokens()
    {
        var path = GetTokenFilePath();

        if (!File.Exists(path))
        {
            return new Dictionary<string, LocalFacebookToken>();
        }

        var json = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, LocalFacebookToken>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, LocalFacebookToken>>(json)
               ?? new Dictionary<string, LocalFacebookToken>();
    }

    private void SaveTokens(Dictionary<string, LocalFacebookToken> tokens)
    {
        var path = GetTokenFilePath();
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(
            tokens,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(path, json);
    }

    private string GetTokenFilePath()
    {
        return Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "facebook-tokens.local.json");
    }

    private sealed class LocalFacebookToken
    {
        public string AccessToken { get; set; } = "";

        public DateTime SavedUtc { get; set; }
    }
}