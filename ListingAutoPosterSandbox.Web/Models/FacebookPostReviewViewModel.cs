using System.ComponentModel.DataAnnotations;

namespace ListingAutoPosterSandbox.Web.Models;

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