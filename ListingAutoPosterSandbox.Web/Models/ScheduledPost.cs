namespace ListingAutoPosterSandbox.Web.Models;

public class ScheduledPost
{
    public int Id { get; set; }

    public int ListingId { get; set; }

    public Listing Listing { get; set; } = null!;

    public PostPlatform Platform { get; set; }

    public int? SocialAccountId { get; set; }

    public SocialAccount? SocialAccount { get; set; }

    public string Caption { get; set; } = string.Empty;

    // Optional image for this specific scheduled post.
    // If this is null or empty, the post should publish as text-only.
    // If this has a value, the publisher may use it for an image/photo post.
    public string? ImageUrl { get; set; }

    public DateTime ScheduledUtc { get; set; }

    public PostStatus Status { get; set; } = PostStatus.Scheduled;

    public string? ExternalPostId { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}