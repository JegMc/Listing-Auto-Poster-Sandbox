using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ListingAutoPosterSandbox.Web.Models;

namespace ListingAutoPosterSandbox.Web.Services;

public class FakePlatformPoster : IPlatformPoster
{
    public Task<PostResult> PublishAsync(
        ScheduledPost scheduledPost,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(new PostResult
            {
                Success = false,
                ErrorMessage = "Access token was missing."
            });
        }

        var externalPostId = $"fake-{scheduledPost.Platform.ToString().ToLower()}-{Guid.NewGuid():N}";

        var tokenFingerprint = CreateTokenFingerprint(accessToken);

        var fakeResponse = new
        {
            platform = scheduledPost.Platform.ToString(),
            socialAccount = scheduledPost.SocialAccount?.DisplayName,
            secretName = scheduledPost.SocialAccount?.SecretName,
            tokenFingerprint,
            externalPostId,
            listingId = scheduledPost.ListingId,
            captionLength = scheduledPost.Caption.Length,
            publishedUtc = DateTime.UtcNow
        };

        var result = new PostResult
        {
            Success = true,
            ExternalPostId = externalPostId,
            ResponseJson = JsonSerializer.Serialize(fakeResponse),
            ErrorMessage = null
        };

        return Task.FromResult(result);
    }

    private static string CreateTokenFingerprint(string accessToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(accessToken);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes)[..12];
    }
}