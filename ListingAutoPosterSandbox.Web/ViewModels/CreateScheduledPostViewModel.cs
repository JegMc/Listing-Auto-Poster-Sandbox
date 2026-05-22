using System.ComponentModel.DataAnnotations;

namespace ListingAutoPosterSandbox.Web.ViewModels;

/// <summary>
/// View model used when the generic scheduling flow creates one or more ScheduledPost rows.
/// This is not a database table. It represents form input from the schedule-post page.
/// </summary>
public class CreateScheduledPostViewModel
{
    [Required]
    public int ListingId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Select at least one connected social account.")]
    public List<int> SocialAccountIds { get; set; } = new();

    [Required]
    [StringLength(2000)]
    public string Caption { get; set; } = string.Empty;

    // User must explicitly choose whether this scheduled post should attach the listing hero image.
    // This prevents every scheduled post from automatically using Listing.ImageUrl.
    public bool IncludeImage { get; set; }

    [Required]
    public DateTime ScheduledLocal { get; set; }
}