using System.Net.Http.Headers;
using System.Text.Json;
using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ListingAutoPosterSandbox.Web.Services;

public sealed class InstagramConnectionDiagnosticService : IInstagramConnectionDiagnosticService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ITokenStore _tokenStore;
    private readonly InstagramDiagnosticOptions _options;
    private readonly ILogger<InstagramConnectionDiagnosticService> _logger;

    public InstagramConnectionDiagnosticService(
        HttpClient httpClient,
        AppDbContext context,
        ITokenStore tokenStore,
        IOptions<InstagramDiagnosticOptions> options,
        ILogger<InstagramConnectionDiagnosticService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _tokenStore = tokenStore;
        _options = options.Value;
        _logger = logger;
    }

    public Task<InstagramConnectionDiagnosticResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        return CheckCoreAsync(
            saveDiscoveredInstagramAccount: false,
            cancellationToken);
    }

    public Task<InstagramConnectionDiagnosticResult> CheckAndSaveAsync(
        CancellationToken cancellationToken = default)
    {
        return CheckCoreAsync(
            saveDiscoveredInstagramAccount: true,
            cancellationToken);
    }

    private async Task<InstagramConnectionDiagnosticResult> CheckCoreAsync(
        bool saveDiscoveredInstagramAccount,
        CancellationToken cancellationToken = default)
    {
        var configuredPageId = _options.FacebookPageId?.Trim();

        if (string.IsNullOrWhiteSpace(configuredPageId))
        {
            return new InstagramConnectionDiagnosticResult
            {
                IsConfigured = false,
                StatusMessage = "Instagram diagnostic is not configured. Add InstagramDiagnostic:FacebookPageId to user-secrets."
            };
        }

        var facebookAccount = await _context.SocialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                account =>
                    account.Platform == PostPlatform.Facebook &&
                    account.IsConnected &&
                    account.PlatformAccountId == configuredPageId,
                cancellationToken);

        if (facebookAccount is null)
        {
            return new InstagramConnectionDiagnosticResult
            {
                IsConfigured = true,
                PageId = configuredPageId,
                StatusMessage = $"No connected Facebook social account was found for Page ID {configuredPageId}. Connect or verify the Facebook Page first."
            };
        }

        string accessToken;

        try
        {
            accessToken = await _tokenStore.GetAccessTokenAsync(
                facebookAccount.SecretName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not load access token for Facebook social account {SocialAccountId}.",
                facebookAccount.Id);

            return new InstagramConnectionDiagnosticResult
            {
                IsConfigured = true,
                PageId = configuredPageId,
                StatusMessage = "Facebook Page account was found, but the access token could not be loaded.",
                ErrorMessage = ex.Message
            };
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new InstagramConnectionDiagnosticResult
            {
                IsConfigured = true,
                PageId = configuredPageId,
                StatusMessage = "Facebook Page account was found, but the stored access token is empty."
            };
        }

        var graphApiVersion = string.IsNullOrWhiteSpace(_options.GraphApiVersion)
            ? "v20.0"
            : _options.GraphApiVersion.Trim();

        var fields = Uri.EscapeDataString("id,name,instagram_business_account{id,username}");
        var pageId = Uri.EscapeDataString(configuredPageId);

        var requestUrl =
            $"https://graph.facebook.com/{graphApiVersion}/{pageId}?fields={fields}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new InstagramConnectionDiagnosticResult
            {
                IsConfigured = true,
                PageId = configuredPageId,
                PageFound = false,
                StatusMessage = "Graph API request failed. The Facebook Page was not verified through the diagnostic call.",
                ErrorMessage = responseJson,
                RawResponseJson = PrettyPrintJson(responseJson)
            };
        }

        var result = ParseSuccessfulResponse(responseJson, configuredPageId);

        if (saveDiscoveredInstagramAccount)
        {
            await SaveDiscoveredInstagramSocialAccountAsync(
                result,
                cancellationToken);
        }

        return result;
    }

    private InstagramConnectionDiagnosticResult ParseSuccessfulResponse(
        string responseJson,
        string configuredPageId)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        var pageId = root.TryGetProperty("id", out var pageIdElement)
            ? pageIdElement.GetString()
            : configuredPageId;

        var pageName = root.TryGetProperty("name", out var pageNameElement)
            ? pageNameElement.GetString()
            : null;

        string? instagramAccountId = null;
        string? instagramUsername = null;

        if (root.TryGetProperty("instagram_business_account", out var instagramElement))
        {
            if (instagramElement.TryGetProperty("id", out var instagramIdElement))
            {
                instagramAccountId = instagramIdElement.GetString();
            }

            if (instagramElement.TryGetProperty("username", out var usernameElement))
            {
                instagramUsername = usernameElement.GetString();
            }
        }

        var expectedUsername = _options.ExpectedInstagramUsername;
        bool? usernameMatches = null;

        if (!string.IsNullOrWhiteSpace(expectedUsername) &&
            !string.IsNullOrWhiteSpace(instagramUsername))
        {
            usernameMatches =
                NormalizeUsername(expectedUsername) == NormalizeUsername(instagramUsername);
        }

        var instagramFound = !string.IsNullOrWhiteSpace(instagramAccountId);

        var statusMessage = BuildStatusMessage(
            pageName,
            instagramFound,
            instagramUsername,
            usernameMatches);

        return new InstagramConnectionDiagnosticResult
        {
            IsConfigured = true,
            PageFound = true,
            PageId = pageId,
            PageName = pageName,
            InstagramAccountFound = instagramFound,
            InstagramAccountId = instagramAccountId,
            InstagramUsername = instagramUsername,
            ExpectedInstagramUsername = expectedUsername,
            ExpectedInstagramUsernameMatches = usernameMatches,
            StatusMessage = statusMessage,
            RawResponseJson = PrettyPrintJson(responseJson)
        };
    }

    private async Task SaveDiscoveredInstagramSocialAccountAsync(
        InstagramConnectionDiagnosticResult result,
        CancellationToken cancellationToken)
    {
        if (!result.PageFound || !result.InstagramAccountFound)
        {
            result.InstagramSocialAccountSaved = false;
            result.InstagramSocialAccountSaveMessage =
                "Instagram social account was not saved because no connected Instagram business account was found.";
            return;
        }

        if (string.IsNullOrWhiteSpace(result.InstagramAccountId))
        {
            result.InstagramSocialAccountSaved = false;
            result.InstagramSocialAccountSaveMessage =
                "Instagram social account was not saved because the Instagram Graph account ID was missing.";
            return;
        }

        var now = DateTime.UtcNow;
        var instagramAccountId = result.InstagramAccountId.Trim();
        var username = result.InstagramUsername?.Trim();

        var socialAccount = await _context.SocialAccounts
            .FirstOrDefaultAsync(
                account =>
                    account.Platform == PostPlatform.Instagram &&
                    account.PlatformAccountId == instagramAccountId,
                cancellationToken);

        socialAccount ??= await _context.SocialAccounts
            .FirstOrDefaultAsync(
                account =>
                    account.Platform == PostPlatform.Instagram &&
                    string.IsNullOrEmpty(account.PlatformAccountId),
                cancellationToken);

        var createdNewAccount = false;

        if (socialAccount is null)
        {
            socialAccount = new SocialAccount
            {
                Platform = PostPlatform.Instagram,
                CreatedUtc = now
            };

            _context.SocialAccounts.Add(socialAccount);
            createdNewAccount = true;
        }

        socialAccount.DisplayName = BuildInstagramDisplayName(username);
        socialAccount.PlatformAccountId = instagramAccountId;
        socialAccount.SecretName = BuildInstagramSecretName(username);
        socialAccount.IsConnected = true;
        socialAccount.UpdatedUtc = now;

        await _context.SaveChangesAsync(cancellationToken);

        result.InstagramSocialAccountSaved = true;
        result.InstagramSocialAccountId = socialAccount.Id;
        result.InstagramSocialAccountSaveMessage = createdNewAccount
            ? $"Created Instagram SocialAccount row for @{NormalizeUsername(username ?? "business-account")}."
            : $"Updated Instagram SocialAccount row for @{NormalizeUsername(username ?? "business-account")}.";
    }

    private static string BuildStatusMessage(
        string? pageName,
        bool instagramFound,
        string? instagramUsername,
        bool? usernameMatches)
    {
        if (!instagramFound)
        {
            return "Facebook Page was found, but no connected Instagram business account was returned.";
        }

        if (usernameMatches == false)
        {
            return $"Instagram account discovered: @{instagramUsername}, but it does not match the expected username.";
        }

        return $"Instagram account discovered: @{instagramUsername}. Connected through Facebook Page: {pageName}. Instagram API publishing is not enabled yet. This sandbox currently verifies the account connection only.";
    }

    private static string BuildInstagramDisplayName(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return "Instagram Business Account (scaffolded)";
        }

        return $"Instagram @{NormalizeUsername(username)} (scaffolded)";
    }

    private static string BuildInstagramSecretName(string? username)
    {
        var normalizedUsername = string.IsNullOrWhiteSpace(username)
            ? "business-account"
            : NormalizeUsername(username);

        return $"dev/social/instagram/{normalizedUsername}";
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().TrimStart('@').ToLowerInvariant();
    }

    private static string PrettyPrintJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            return JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }
        catch
        {
            return json;
        }
    }
}