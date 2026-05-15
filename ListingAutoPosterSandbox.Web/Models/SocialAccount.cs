namespace ListingAutoPosterSandbox.Web.Models;

public class SocialAccount
{
    public int Id { get; set; }

    public PostPlatform Platform { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string SecretName { get; set; } = string.Empty;

    public bool IsConnected { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}