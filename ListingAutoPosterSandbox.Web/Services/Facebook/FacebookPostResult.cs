namespace ListingAutoPosterSandbox.Web.Services;

public sealed class FacebookPostResult
{
    public bool Success { get; init; }

    public string? PostId { get; init; }

    public int StatusCode { get; init; }

    public string RawResponse { get; init; } = "";

    public string? ErrorMessage { get; init; }

    public static FacebookPostResult Succeeded(
        string? postId,
        int statusCode,
        string rawResponse)
    {
        return new FacebookPostResult
        {
            Success = true,
            PostId = postId,
            StatusCode = statusCode,
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
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            RawResponse = rawResponse
        };
    }
}