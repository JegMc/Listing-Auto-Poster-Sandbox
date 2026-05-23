using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ListingAutoPosterSandbox.Web.Models;
using Microsoft.Extensions.Options;
namespace ListingAutoPosterSandbox.Web.Services.Facebook;

public sealed class FacebookPagePoster : IPlatformPoster
{
    public PostPlatform Platform => PostPlatform.Facebook;

    private readonly HttpClient _httpClient;
    private readonly FacebookOptions _options;
    private readonly IWebHostEnvironment _environment;

    public FacebookPagePoster(
        HttpClient httpClient,
        IOptions<FacebookOptions> options,
        IWebHostEnvironment environment)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _environment = environment;
    }

    public async Task<PostResult> PublishAsync(
        ScheduledPost scheduledPost,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (scheduledPost.Platform != PostPlatform.Facebook)
        {
            return new PostResult
            {
                Success = false,
                ErrorMessage = $"FacebookPagePoster cannot publish platform '{scheduledPost.Platform}'."
            };
        }

        var pageId = scheduledPost.SocialAccount?.PlatformAccountId;

        if (string.IsNullOrWhiteSpace(pageId))
        {
            return new PostResult
            {
                Success = false,
                ErrorMessage = "Facebook Page ID is missing. Set SocialAccount.PlatformAccountId for the connected Facebook account."
            };
        }

        if (string.IsNullOrWhiteSpace(scheduledPost.ImageUrl))
        {
            var textResult = await PublishTextPostCoreAsync(
                message: scheduledPost.Caption,
                pageId: pageId,
                accessToken: accessToken,
                cancellationToken: cancellationToken);

            return new PostResult
            {
                Success = textResult.Success,
                ExternalPostId = textResult.PostId,
                ResponseJson = textResult.RawResponse,
                ErrorMessage = textResult.ErrorMessage
            };
        }

        var photoResult = await PublishPhotoPostCoreAsync(
            caption: scheduledPost.Caption,
            imageUrlOrPath: scheduledPost.ImageUrl,
            pageId: pageId,
            accessToken: accessToken,
            cancellationToken: cancellationToken);

        return new PostResult
        {
            Success = photoResult.Success,
            ExternalPostId = photoResult.PostId,
            ResponseJson = photoResult.RawResponse,
            ErrorMessage = photoResult.ErrorMessage
        };
    }

    private async Task<FacebookPostResult> PublishTextPostCoreAsync(
        string message,
        string pageId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return FacebookPostResult.Failed(
                statusCode: 400,
                errorMessage: "Message cannot be empty.",
                rawResponse: "");
        }

        var trimmedMessage = message.Trim();

        if (trimmedMessage.Length > 5000)
        {
            return FacebookPostResult.Failed(
                statusCode: 400,
                errorMessage: "Message is too long. Keep the post under 5,000 characters.",
                rawResponse: "");
        }

        if (string.IsNullOrWhiteSpace(_options.GraphApiVersion))
        {
            return FacebookPostResult.Failed(
                statusCode: 500,
                errorMessage: "Missing Facebook:GraphApiVersion.",
                rawResponse: "");
        }

        if (string.IsNullOrWhiteSpace(pageId))
        {
            return FacebookPostResult.Failed(
                statusCode: 500,
                errorMessage: "Missing Facebook Page ID.",
                rawResponse: "");
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return FacebookPostResult.Failed(
                statusCode: 500,
                errorMessage: "Missing Facebook access token.",
                rawResponse: "");
        }

        var requestUrl = $"https://graph.facebook.com/{_options.GraphApiVersion}/{pageId}/feed";

        using var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["message"] = trimmedMessage,
            ["access_token"] = accessToken
        });

        using var response = await _httpClient.PostAsync(
            requestUrl,
            formContent,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var statusCode = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            return FacebookPostResult.Failed(
                statusCode: statusCode,
                errorMessage: ExtractFacebookErrorMessage(responseBody),
                rawResponse: responseBody);
        }

        return FacebookPostResult.Succeeded(
            postId: ExtractPostId(responseBody),
            statusCode: statusCode,
            rawResponse: responseBody);
    }

        private async Task<FacebookPostResult> PublishPhotoPostCoreAsync(
        string caption,
        string imageUrlOrPath,
        string pageId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(caption))
        {
            return FacebookPostResult.Failed(
                statusCode: 400,
                errorMessage: "Caption cannot be empty.",
                rawResponse: "");
        }

        if (string.IsNullOrWhiteSpace(_options.GraphApiVersion))
        {
            return FacebookPostResult.Failed(
                statusCode: 500,
                errorMessage: "Missing Facebook:GraphApiVersion.",
                rawResponse: "");
        }

        if (string.IsNullOrWhiteSpace(pageId))
        {
            return FacebookPostResult.Failed(
                statusCode: 500,
                errorMessage: "Missing Facebook Page ID.",
                rawResponse: "");
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return FacebookPostResult.Failed(
                statusCode: 500,
                errorMessage: "Missing Facebook access token.",
                rawResponse: "");
        }

        var requestUrl = $"https://graph.facebook.com/{_options.GraphApiVersion}/{pageId}/photos";

        if (IsPublicImageUrl(imageUrlOrPath))
        {
            using var publicUrlFormContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["url"] = imageUrlOrPath.Trim(),
                ["caption"] = caption.Trim(),
                ["access_token"] = accessToken
            });

            using var publicUrlResponse = await _httpClient.PostAsync(
                requestUrl,
                publicUrlFormContent,
                cancellationToken);

            var publicUrlResponseBody = await publicUrlResponse.Content.ReadAsStringAsync(cancellationToken);
            var publicUrlStatusCode = (int)publicUrlResponse.StatusCode;

            if (!publicUrlResponse.IsSuccessStatusCode)
            {
                return FacebookPostResult.Failed(
                    statusCode: publicUrlStatusCode,
                    errorMessage: ExtractFacebookErrorMessage(publicUrlResponseBody),
                    rawResponse: publicUrlResponseBody);
            }

            return FacebookPostResult.Succeeded(
                postId: ExtractPostId(publicUrlResponseBody),
                statusCode: publicUrlStatusCode,
                rawResponse: publicUrlResponseBody);
        }

        var localImagePath = TryGetLocalImagePath(imageUrlOrPath);

        if (localImagePath is null || !System.IO.File.Exists(localImagePath))
        {
            return FacebookPostResult.Failed(
                statusCode: 400,
                errorMessage: "The scheduled post image is not a public URL and could not be found as a local uploaded file.",
                rawResponse: "");
        }

        await using var fileStream = System.IO.File.OpenRead(localImagePath);

        using var multipartContent = new MultipartFormDataContent();

        multipartContent.Add(
            new StringContent(caption.Trim()),
            "caption");

        multipartContent.Add(
            new StringContent(accessToken),
            "access_token");

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            GetImageContentType(localImagePath));

        multipartContent.Add(
            fileContent,
            "source",
            Path.GetFileName(localImagePath));

        using var localUploadResponse = await _httpClient.PostAsync(
            requestUrl,
            multipartContent,
            cancellationToken);

        var localUploadResponseBody = await localUploadResponse.Content.ReadAsStringAsync(cancellationToken);
        var localUploadStatusCode = (int)localUploadResponse.StatusCode;

        if (!localUploadResponse.IsSuccessStatusCode)
        {
            return FacebookPostResult.Failed(
                statusCode: localUploadStatusCode,
                errorMessage: ExtractFacebookErrorMessage(localUploadResponseBody),
                rawResponse: localUploadResponseBody);
        }

        return FacebookPostResult.Succeeded(
            postId: ExtractPostId(localUploadResponseBody),
            statusCode: localUploadStatusCode,
            rawResponse: localUploadResponseBody);
    }

    private static bool IsPublicImageUrl(string imageUrlOrPath)
    {
        return Uri.TryCreate(imageUrlOrPath, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private string? TryGetLocalImagePath(string imageUrlOrPath)
    {
        if (!imageUrlOrPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var relativePath = imageUrlOrPath
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(
            Path.Combine(webRootPath, relativePath));

        var normalizedWebRootPath = Path.GetFullPath(webRootPath);

        if (!fullPath.StartsWith(normalizedWebRootPath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }

    private static string GetImageContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    private static string? ExtractPostId(string responseBody)
    {
        try
        {
            using var json = JsonDocument.Parse(responseBody);

            if (json.RootElement.TryGetProperty("id", out var idProperty))
            {
                return idProperty.GetString();
            }

            if (json.RootElement.TryGetProperty("post_id", out var postIdProperty))
            {
                return postIdProperty.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ExtractFacebookErrorMessage(string responseBody)
    {
        try
        {
            using var json = JsonDocument.Parse(responseBody);

            if (json.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.TryGetProperty("message", out var messageElement))
                {
                    var message = messageElement.GetString();

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return message;
                    }
                }

                return errorElement.ToString();
            }

            return responseBody;
        }
        catch (JsonException)
        {
            return responseBody;
        }
    }

    private sealed class FacebookPostResult
    {
        public bool Success { get; private init; }

        public string? PostId { get; private init; }

        public string? ErrorMessage { get; private init; }

        public string RawResponse { get; private init; } = string.Empty;

        public static FacebookPostResult Succeeded(
            string? postId,
            int statusCode,
            string rawResponse)
        {
            return new FacebookPostResult
            {
                Success = true,
                PostId = postId,
                RawResponse = rawResponse
            };
        }

        public static FacebookPostResult Failed(
            int statusCode,
            string errorMessage,
            string rawResponse)
        {
            return new FacebookPostResult
            {
                Success = false,
                ErrorMessage = $"{errorMessage} Status code: {statusCode}.",
                RawResponse = rawResponse
            };
        }
    }
}