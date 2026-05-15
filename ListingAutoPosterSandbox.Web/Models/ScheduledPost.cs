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

    public DateTime ScheduledUtc { get; set; }

    public PostStatus Status { get; set; } = PostStatus.Scheduled;

    public string? ExternalPostId { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}