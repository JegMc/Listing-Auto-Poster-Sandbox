using System.Net.Http.Headers;
using System.Text.Json;
using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ListingAutoPosterSandbox.Web.Services;

public sealed class InstagramPlatformPoster : IPlatformPoster
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ITokenStore _tokenStore;
    private readonly InstagramDiagnosticOptions _options;
    private readonly ILogger<InstagramPlatformPoster> _logger;

    public InstagramPlatformPoster(
        HttpClient httpClient,
        AppDbContext context,
        ITokenStore tokenStore,
        IOptions<InstagramDiagnosticOptions> options,
        ILogger<InstagramPlatformPoster> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _tokenStore = tokenStore;
        _options = options.Value;
        _logger = logger;
    }

    public PostPlatform Platform => PostPlatform.Instagram;

    public async Task<PostResult> PublishAsync(
        ScheduledPost scheduledPost,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var readiness = InstagramPostReadinessValidator.Check(scheduledPost);

        if (!readiness.IsReady)
        {
            return CreateFailedResult(
                scheduledPost,
                readiness.Message,
                new
                {
                    success = false,
                    mode = "RealInstagramPublishingReadinessCheck",
                    platform = "Instagram",
                    scheduledPostId = scheduledPost.Id,
                    message = readiness.Message,
                    noInstagramApiCallWasMade = true,
                    checkedUtc = DateTime.UtcNow
                });
        }

        var instagramAccountId = scheduledPost.SocialAccount?.PlatformAccountId;

        if (string.IsNullOrWhiteSpace(instagramAccountId))
        {
            return CreateFailedResult(
                scheduledPost,
                "Instagram publishing failed before calling the API: the Instagram SocialAccount is missing PlatformAccountId.",
                new
                {
                    success = false,
                    mode = "RealInstagramPublishing",
                    platform = "Instagram",
                    scheduledPostId = scheduledPost.Id,
                    message = "Instagram SocialAccount.PlatformAccountId is required. Save the discovered Instagram account from the diagnostic page first.",
                    noInstagramApiCallWasMade = true,
                    checkedUtc = DateTime.UtcNow
                });
        }

        var facebookAccessTokenResult = await GetFacebookPageAccessTokenAsync(cancellationToken);

        if (!facebookAccessTokenResult.Success)
        {
            return CreateFailedResult(
                scheduledPost,
                facebookAccessTokenResult.ErrorMessage ?? "Could not resolve Facebook Page access token for Instagram publishing.",
                new
                {
                    success = false,
                    mode = "RealInstagramPublishing",
                    platform = "Instagram",
                    scheduledPostId = scheduledPost.Id,
                    message = facebookAccessTokenResult.ErrorMessage,
                    noInstagramApiCallWasMade = true,
                    checkedUtc = DateTime.UtcNow
                });
        }

        var graphApiVersion = string.IsNullOrWhiteSpace(_options.GraphApiVersion)
            ? "v20.0"
            : _options.GraphApiVersion.Trim();

        try
        {
            var caption = scheduledPost.Caption?.Trim() ?? string.Empty;

            var containerResult = await CreateMediaContainerAsync(
                graphApiVersion,
                instagramAccountId.Trim(),
                readiness.PublicImageUrl!,
                caption,
                facebookAccessTokenResult.AccessToken!,
                cancellationToken);

            if (!containerResult.Success)
            {
                return CreateFailedResult(
                    scheduledPost,
                    containerResult.ErrorMessage ?? "Instagram media container creation failed.",
                    new
                    {
                        success = false,
                        mode = "RealInstagramPublishing",
                        platform = "Instagram",
                        step = "CreateMediaContainer",
                        scheduledPostId = scheduledPost.Id,
                        instagramAccountId,
                        publicImageUrl = readiness.PublicImageUrl,
                        error = containerResult.ErrorMessage,
                        rawResponseJson = containerResult.RawResponseJson,
                        checkedUtc = DateTime.UtcNow
                    });
            }

            var readinessPollResult = await WaitForContainerToFinishAsync(
                graphApiVersion,
                containerResult.ContainerId!,
                facebookAccessTokenResult.AccessToken!,
                cancellationToken);

            if (!readinessPollResult.Success)
            {
                return CreateFailedResult(
                    scheduledPost,
                    readinessPollResult.ErrorMessage ?? "Instagram media container did not become ready for publishing.",
                    new
                    {
                        success = false,
                        mode = "RealInstagramPublishing",
                        platform = "Instagram",
                        step = "PollMediaContainer",
                        scheduledPostId = scheduledPost.Id,
                        instagramAccountId,
                        containerId = containerResult.ContainerId,
                        error = readinessPollResult.ErrorMessage,
                        rawResponseJson = readinessPollResult.RawResponseJson,
                        checkedUtc = DateTime.UtcNow
                    });
            }

            var publishResult = await PublishMediaContainerAsync(
                graphApiVersion,
                instagramAccountId.Trim(),
                containerResult.ContainerId!,
                facebookAccessTokenResult.AccessToken!,
                cancellationToken);

            if (!publishResult.Success)
            {
                return CreateFailedResult(
                    scheduledPost,
                    publishResult.ErrorMessage ?? "Instagram media publish failed.",
                    new
                    {
                        success = false,
                        mode = "RealInstagramPublishing",
                        platform = "Instagram",
                        step = "PublishMediaContainer",
                        scheduledPostId = scheduledPost.Id,
                        instagramAccountId,
                        containerId = containerResult.ContainerId,
                        error = publishResult.ErrorMessage,
                        rawResponseJson = publishResult.RawResponseJson,
                        checkedUtc = DateTime.UtcNow
                    });
            }

            var permalinkResult = await TryGetPermalinkAsync(
                graphApiVersion,
                publishResult.MediaId!,
                facebookAccessTokenResult.AccessToken!,
                cancellationToken);

            var successResponse = new
            {
                success = true,
                mode = "RealInstagramPublishing",
                platform = "Instagram",
                scheduledPostId = scheduledPost.Id,
                instagramAccountId,
                containerId = containerResult.ContainerId,
                instagramMediaId = publishResult.MediaId,
                permalink = permalinkResult.Permalink,
                publicImageUrl = readiness.PublicImageUrl,
                message = "Instagram image post published successfully.",
                publishedUtc = DateTime.UtcNow
            };

            return new PostResult
            {
                Success = true,
                ExternalPostId = publishResult.MediaId,
                ResponseJson = JsonSerializer.Serialize(successResponse),
                ErrorMessage = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error publishing scheduled post {ScheduledPostId} to Instagram.",
                scheduledPost.Id);

            return CreateFailedResult(
                scheduledPost,
                $"Unexpected Instagram publishing error: {ex.Message}",
                new
                {
                    success = false,
                    mode = "RealInstagramPublishing",
                    platform = "Instagram",
                    scheduledPostId = scheduledPost.Id,
                    message = ex.Message,
                    checkedUtc = DateTime.UtcNow
                });
        }
    }

    private async Task<FacebookAccessTokenResult> GetFacebookPageAccessTokenAsync(
        CancellationToken cancellationToken)
    {
        var configuredPageId = _options.FacebookPageId?.Trim();

        if (string.IsNullOrWhiteSpace(configuredPageId))
        {
            return FacebookAccessTokenResult.Fail(
                "Instagram publishing requires InstagramDiagnostic:FacebookPageId to be configured.");
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
            return FacebookAccessTokenResult.Fail(
                $"No connected Facebook SocialAccount was found for Page ID {configuredPageId}.");
        }

        var token = await _tokenStore.GetAccessTokenAsync(
            facebookAccount.SecretName,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            return FacebookAccessTokenResult.Fail(
                "The configured Facebook SocialAccount token was empty.");
        }

        return FacebookAccessTokenResult.Ok(token);
    }

    private async Task<CreateContainerResult> CreateMediaContainerAsync(
        string graphApiVersion,
        string instagramAccountId,
        string imageUrl,
        string caption,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var requestUrl =
            $"https://graph.facebook.com/{graphApiVersion}/{Uri.EscapeDataString(instagramAccountId)}/media";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["image_url"] = imageUrl,
            ["caption"] = caption
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return CreateContainerResult.Fail(
                $"Instagram media container creation failed with HTTP {(int)response.StatusCode}: {ExtractGraphErrorMessage(responseJson)}",
                PrettyPrintJson(responseJson));
        }

        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (!root.TryGetProperty("id", out var idElement))
        {
            return CreateContainerResult.Fail(
                "Instagram media container creation succeeded but no container id was returned.",
                PrettyPrintJson(responseJson));
        }

        var containerId = idElement.GetString();

        if (string.IsNullOrWhiteSpace(containerId))
        {
            return CreateContainerResult.Fail(
                "Instagram media container id was empty.",
                PrettyPrintJson(responseJson));
        }

        return CreateContainerResult.Ok(containerId, PrettyPrintJson(responseJson));
    }

    private async Task<ContainerStatusResult> WaitForContainerToFinishAsync(
        string graphApiVersion,
        string containerId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 8;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var statusResult = await GetContainerStatusAsync(
                graphApiVersion,
                containerId,
                accessToken,
                cancellationToken);

            if (!statusResult.Success)
            {
                return statusResult;
            }

            if (string.Equals(statusResult.StatusCode, "FINISHED", StringComparison.OrdinalIgnoreCase))
            {
                return statusResult;
            }

            if (string.Equals(statusResult.StatusCode, "ERROR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(statusResult.StatusCode, "EXPIRED", StringComparison.OrdinalIgnoreCase))
            {
                return ContainerStatusResult.Fail(
                    $"Instagram media container status is {statusResult.StatusCode}.",
                    statusResult.RawResponseJson);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        return ContainerStatusResult.Fail(
            "Instagram media container did not reach FINISHED status before the polling limit.",
            null);
    }

    private async Task<ContainerStatusResult> GetContainerStatusAsync(
        string graphApiVersion,
        string containerId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var requestUrl =
            $"https://graph.facebook.com/{graphApiVersion}/{Uri.EscapeDataString(containerId)}?fields=status_code";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ContainerStatusResult.Fail(
                $"Instagram media container status check failed with HTTP {(int)response.StatusCode}: {ExtractGraphErrorMessage(responseJson)}",
                PrettyPrintJson(responseJson));
        }

        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        var statusCode = root.TryGetProperty("status_code", out var statusElement)
            ? statusElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(statusCode))
        {
            return ContainerStatusResult.Fail(
                "Instagram media container status response did not include status_code.",
                PrettyPrintJson(responseJson));
        }

        return ContainerStatusResult.Ok(statusCode, PrettyPrintJson(responseJson));
    }

    private async Task<PublishContainerResult> PublishMediaContainerAsync(
        string graphApiVersion,
        string instagramAccountId,
        string containerId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var requestUrl =
            $"https://graph.facebook.com/{graphApiVersion}/{Uri.EscapeDataString(instagramAccountId)}/media_publish";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["creation_id"] = containerId
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return PublishContainerResult.Fail(
                $"Instagram media publish failed with HTTP {(int)response.StatusCode}: {ExtractGraphErrorMessage(responseJson)}",
                PrettyPrintJson(responseJson));
        }

        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (!root.TryGetProperty("id", out var idElement))
        {
            return PublishContainerResult.Fail(
                "Instagram media publish succeeded but no media id was returned.",
                PrettyPrintJson(responseJson));
        }

        var mediaId = idElement.GetString();

        if (string.IsNullOrWhiteSpace(mediaId))
        {
            return PublishContainerResult.Fail(
                "Instagram media id was empty.",
                PrettyPrintJson(responseJson));
        }

        return PublishContainerResult.Ok(mediaId, PrettyPrintJson(responseJson));
    }

    private async Task<PermalinkResult> TryGetPermalinkAsync(
        string graphApiVersion,
        string mediaId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var requestUrl =
            $"https://graph.facebook.com/{graphApiVersion}/{Uri.EscapeDataString(mediaId)}?fields=id,permalink";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new PermalinkResult(null, PrettyPrintJson(responseJson));
        }

        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        var permalink = root.TryGetProperty("permalink", out var permalinkElement)
            ? permalinkElement.GetString()
            : null;

        return new PermalinkResult(permalink, PrettyPrintJson(responseJson));
    }

    private static PostResult CreateFailedResult(
        ScheduledPost scheduledPost,
        string errorMessage,
        object responseObject)
    {
        return new PostResult
        {
            Success = false,
            ExternalPostId = null,
            ResponseJson = JsonSerializer.Serialize(responseObject),
            ErrorMessage = errorMessage
        };
    }

    private static string ExtractGraphErrorMessage(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return "No response body returned.";
        }

        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            if (root.TryGetProperty("error", out var errorElement))
            {
                var message = errorElement.TryGetProperty("message", out var messageElement)
                    ? messageElement.GetString()
                    : null;

                var type = errorElement.TryGetProperty("type", out var typeElement)
                    ? typeElement.GetString()
                    : null;

                var code = errorElement.TryGetProperty("code", out var codeElement)
                    ? codeElement.ToString()
                    : null;

                return $"Graph API error: {message ?? "Unknown message"} Type={type ?? "unknown"} Code={code ?? "unknown"}";
            }
        }
        catch
        {
            // Fall through and return the raw response.
        }

        return responseJson;
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

    private sealed record FacebookAccessTokenResult(
        bool Success,
        string? AccessToken,
        string? ErrorMessage)
    {
        public static FacebookAccessTokenResult Ok(string accessToken)
        {
            return new FacebookAccessTokenResult(true, accessToken, null);
        }

        public static FacebookAccessTokenResult Fail(string errorMessage)
        {
            return new FacebookAccessTokenResult(false, null, errorMessage);
        }
    }

    private sealed record CreateContainerResult(
        bool Success,
        string? ContainerId,
        string? ErrorMessage,
        string? RawResponseJson)
    {
        public static CreateContainerResult Ok(string containerId, string rawResponseJson)
        {
            return new CreateContainerResult(true, containerId, null, rawResponseJson);
        }

        public static CreateContainerResult Fail(string errorMessage, string? rawResponseJson)
        {
            return new CreateContainerResult(false, null, errorMessage, rawResponseJson);
        }
    }

    private sealed record ContainerStatusResult(
        bool Success,
        string? StatusCode,
        string? ErrorMessage,
        string? RawResponseJson)
    {
        public static ContainerStatusResult Ok(string statusCode, string rawResponseJson)
        {
            return new ContainerStatusResult(true, statusCode, null, rawResponseJson);
        }

        public static ContainerStatusResult Fail(string errorMessage, string? rawResponseJson)
        {
            return new ContainerStatusResult(false, null, errorMessage, rawResponseJson);
        }
    }

    private sealed record PublishContainerResult(
        bool Success,
        string? MediaId,
        string? ErrorMessage,
        string? RawResponseJson)
    {
        public static PublishContainerResult Ok(string mediaId, string rawResponseJson)
        {
            return new PublishContainerResult(true, mediaId, null, rawResponseJson);
        }

        public static PublishContainerResult Fail(string errorMessage, string? rawResponseJson)
        {
            return new PublishContainerResult(false, null, errorMessage, rawResponseJson);
        }
    }

    private sealed record PermalinkResult(
        string? Permalink,
        string? RawResponseJson);
}