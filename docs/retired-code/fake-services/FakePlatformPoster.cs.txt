using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ListingAutoPosterSandbox.Web.Models;

namespace ListingAutoPosterSandbox.Web.Services.Development;

/// <summary>
/// Development-only fake implementation of IPlatformPoster.
/// 
/// This class does not post to Facebook. It creates a fake successful publish result.
/// It is useful for learning, debugging the ScheduledPost pipeline, or testing without
/// calling the real Meta Graph API.
/// 
/// This class is intentionally not registered in Program.cs.
/// The active production-like path uses FacebookPagePoster.
/// </summary>
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

        return Task.FromResult(new PostResult
        {
            Success = true,
            ExternalPostId = externalPostId,
            ResponseJson = JsonSerializer.Serialize(fakeResponse),
            ErrorMessage = null
        });
    }

    private static string CreateTokenFingerprint(string accessToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(accessToken);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes)[..12];
    }
}