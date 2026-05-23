namespace ListingAutoPosterSandbox.Web.Services;

public sealed class InstagramPostReadinessResult
{
    public bool IsReady { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? PublicImageUrl { get; set; }
}