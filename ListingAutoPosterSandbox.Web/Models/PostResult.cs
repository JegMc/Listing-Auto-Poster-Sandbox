namespace ListingAutoPosterSandbox.Web.Models;

public class PostResult
{
    public bool Success { get; set; }

    public string? ExternalPostId { get; set; }

    public string? ResponseJson { get; set; }

    public string? ErrorMessage { get; set; }
}