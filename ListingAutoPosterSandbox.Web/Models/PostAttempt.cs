namespace ListingAutoPosterSandbox.Web.Models;

public class PostAttempt
{
    public int Id { get; set; }

    public int ScheduledPostId { get; set; }

    public ScheduledPost ScheduledPost { get; set; } = null!;

    public DateTime StartedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ResponseJson { get; set; }
}