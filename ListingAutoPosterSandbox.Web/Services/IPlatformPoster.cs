using ListingAutoPosterSandbox.Web.Models;

namespace ListingAutoPosterSandbox.Web.Services;

public interface IPlatformPoster
{
    PostPlatform Platform { get; }

    Task<PostResult> PublishAsync(
        ScheduledPost scheduledPost,
        string accessToken,
        CancellationToken cancellationToken = default);
}