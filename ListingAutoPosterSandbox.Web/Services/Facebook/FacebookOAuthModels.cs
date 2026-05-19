using System.Text.Json.Serialization;

namespace ListingAutoPosterSandbox.Web.Services;

public sealed class FacebookTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }
}

public sealed class FacebookAccountsResponse
{
    [JsonPropertyName("data")]
    public List<FacebookPageAccount> Data { get; set; } = new();
}

public sealed class FacebookPageAccount
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("tasks")]
    public List<string>? Tasks { get; set; }
}