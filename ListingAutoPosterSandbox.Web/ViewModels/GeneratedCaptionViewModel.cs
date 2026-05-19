using ListingAutoPosterSandbox.Web.Models;

namespace ListingAutoPosterSandbox.Web.ViewModels;

/// <summary>
/// View model for the older generic caption-generation page.
/// 
/// This is still active while the app supports the "Generate Caption" flow.
/// It is separate from the newer Facebook-specific review flow.
/// </summary>
public class GeneratedCaptionViewModel
{
    public Listing Listing { get; set; } = new();

    public string Caption { get; set; } = string.Empty;

    public List<SocialAccount> SocialAccounts { get; set; } = new();
}