namespace ListingAutoPosterSandbox.Web.Models;

public class GeneratedCaptionViewModel
{
    public Listing Listing { get; set; } = new();

    public string Caption { get; set; } = string.Empty;

    public List<SocialAccount> SocialAccounts { get; set; } = new();
}