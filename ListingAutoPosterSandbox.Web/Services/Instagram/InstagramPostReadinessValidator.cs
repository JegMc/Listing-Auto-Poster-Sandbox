using ListingAutoPosterSandbox.Web.Models;

namespace ListingAutoPosterSandbox.Web.Services;

public static class InstagramPostReadinessValidator
{
    public static InstagramPostReadinessResult Check(ScheduledPost scheduledPost)
    {
        if (string.IsNullOrWhiteSpace(scheduledPost.ImageUrl))
        {
            return NotReady(
                "Instagram scaffold validation failed: Instagram feed publishing requires an image. Attach a public image URL before this can become a real Instagram post.");
        }

        var imageUrl = scheduledPost.ImageUrl.Trim();

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return NotReady(
                "Instagram scaffold validation failed: Instagram requires an absolute public image URL. Relative local upload paths cannot be used by the Instagram API.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return NotReady(
                "Instagram scaffold validation failed: Instagram should use a public HTTPS image URL. Use an HTTPS image URL before attempting real Instagram publishing.");
        }

        if (uri.IsLoopback ||
            string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.StartsWith("127.", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return NotReady(
                "Instagram scaffold validation failed: localhost and local-network images are not reachable by Instagram. Use a publicly accessible hosted image URL.");
        }

        return new InstagramPostReadinessResult
        {
            IsReady = true,
            PublicImageUrl = imageUrl,
            Message = "Instagram scaffold validation passed. This post has a public HTTPS image URL and is shaped like a future Instagram image post."
        };
    }

    private static InstagramPostReadinessResult NotReady(string message)
    {
        return new InstagramPostReadinessResult
        {
            IsReady = false,
            Message = message
        };
    }
}