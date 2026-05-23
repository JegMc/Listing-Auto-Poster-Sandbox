using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ListingAutoPosterSandbox.Web.Services;

public sealed class FacebookOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly FacebookOptions _options;

    public FacebookOAuthService(
        HttpClient httpClient,
        IOptions<FacebookOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string BuildAuthorizationUrl(
        string redirectUri,
        string state)
    {
        EnsureConfigured();

        var scopes = string.Join(
            ",",
            "pages_show_list",
            "pages_read_engagement",
            "pages_manage_posts",
            "instagram_basic",
            "instagram_content_publish");

        var baseUrl =
            $"https://www.facebook.com/{_options.GraphApiVersion}/dialog/oauth";

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.AppId,
            ["redirect_uri"] = redirectUri,
            ["state"] = state,
            ["scope"] = scopes,
            ["response_type"] = "code"
        };

        return QueryHelpers.AddQueryString(baseUrl, query);
    }

    public async Task<FacebookTokenResponse> ExchangeCodeForShortLivedTokenAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var url = QueryHelpers.AddQueryString(
            $"https://graph.facebook.com/{_options.GraphApiVersion}/oauth/access_token",
            new Dictionary<string, string?>
            {
                ["client_id"] = _options.AppId,
                ["client_secret"] = _options.AppSecret,
                ["redirect_uri"] = redirectUri,
                ["code"] = code
            });

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Facebook short-lived token exchange failed. Status {(int)response.StatusCode}. Response: {body}");
        }

        return await response.Content.ReadFromJsonAsync<FacebookTokenResponse>(
                   cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Facebook returned an empty short-lived token response.");
    }

    public async Task<FacebookTokenResponse> ExchangeForLongLivedTokenAsync(
        string shortLivedAccessToken,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var url = QueryHelpers.AddQueryString(
            $"https://graph.facebook.com/{_options.GraphApiVersion}/oauth/access_token",
            new Dictionary<string, string?>
            {
                ["grant_type"] = "fb_exchange_token",
                ["client_id"] = _options.AppId,
                ["client_secret"] = _options.AppSecret,
                ["fb_exchange_token"] = shortLivedAccessToken
            });

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Facebook long-lived token exchange failed. Status {(int)response.StatusCode}. Response: {body}");
        }

        return await response.Content.ReadFromJsonAsync<FacebookTokenResponse>(
                   cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Facebook returned an empty long-lived token response.");
    }

    public async Task<List<FacebookPageAccount>> GetPagesAsync(
        string userAccessToken,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var url = QueryHelpers.AddQueryString(
            $"https://graph.facebook.com/{_options.GraphApiVersion}/me/accounts",
            new Dictionary<string, string?>
            {
                ["fields"] = "id,name,access_token,tasks",
                ["access_token"] = userAccessToken
            });

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Facebook Page lookup failed. Status {(int)response.StatusCode}. Response: {body}");
        }

        var accounts = await response.Content.ReadFromJsonAsync<FacebookAccountsResponse>(
            cancellationToken: cancellationToken);

        return accounts?.Data ?? new List<FacebookPageAccount>();
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.AppId))
        {
            throw new InvalidOperationException("Missing Facebook:AppId.");
        }

        if (string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            throw new InvalidOperationException("Missing Facebook:AppSecret.");
        }

        if (string.IsNullOrWhiteSpace(_options.GraphApiVersion))
        {
            throw new InvalidOperationException("Missing Facebook:GraphApiVersion.");
        }
    }
}