using System.ComponentModel.DataAnnotations;

namespace ListingAutoPosterSandbox.Web.ViewModels;

/// <summary>
/// View model for the Facebook post review page.
/// 
/// This is not a database table. It only carries data between the controller
/// and the Razor view so the user can review/edit the AI-generated post before publishing.
/// </summary>
public sealed class FacebookPostReviewViewModel
{
    [Required]
    public int ListingId { get; set; }

    public string ListingTitle { get; set; } = "";

    public string ListingAddress { get; set; } = "";

    public string ListingDescription { get; set; } = "";

    public string? ListingImageUrl { get; set; }

    public decimal ListingPrice { get; set; }

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    [Display(Name = "Facebook post text")]
    public string Caption { get; set; } = "";
}