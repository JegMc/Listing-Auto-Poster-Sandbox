using System.Text.Json;
using ListingAutoPosterSandbox.Web.Models;
using Microsoft.Extensions.Options;

namespace ListingAutoPosterSandbox.Web.Services;

public sealed class FacebookPagePoster : IPlatformPoster
{
    private readonly HttpClient _httpClient;
    private readonly FacebookOptions _options;

    public FacebookPagePoster(
        HttpClient httpClient,
        IOptions<FacebookOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
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
                ErrorMessage =
                    "Facebook Page ID is missing. Set SocialAccount.PlatformAccountId for the connected Facebook account."
            };
        }

        var facebookResult = await PublishTextPostCoreAsync(
            message: scheduledPost.Caption,
            pageId: pageId,
            accessToken: accessToken,
            cancellationToken: cancellationToken);

        return new PostResult
        {
            Success = facebookResult.Success,
            ExternalPostId = facebookResult.PostId,
            ResponseJson = facebookResult.RawResponse,
            ErrorMessage = facebookResult.ErrorMessage
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

        var requestUrl =
            $"https://graph.facebook.com/{_options.GraphApiVersion}/{pageId}/feed";

        var formValues = new Dictionary<string, string>
        {
            ["message"] = trimmedMessage,
            ["access_token"] = accessToken
        };

        using var response = await _httpClient.PostAsync(
            requestUrl,
            new FormUrlEncodedContent(formValues),
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

        var postId = ExtractPostId(responseBody);

        return FacebookPostResult.Succeeded(
            postId: postId,
            statusCode: statusCode,
            rawResponse: responseBody);
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

            if (json.RootElement.TryGetProperty("error", out var errorElement) &&
                errorElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString() ?? "Facebook returned an error.";
            }

            return responseBody;
        }
        catch (JsonException)
        {
            return responseBody;
        }
    }
}