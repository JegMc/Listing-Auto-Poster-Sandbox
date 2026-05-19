using System.ComponentModel.DataAnnotations;

namespace ListingAutoPosterSandbox.Web.ViewModels;

/// <summary>
/// View model used when the older generic scheduling flow creates a ScheduledPost.
/// 
/// This is not a database table. It represents form input from the schedule-post page.
/// </summary>
public class CreateScheduledPostViewModel
{
    [Required]
    public int ListingId { get; set; }

    [Required]
    public int SocialAccountId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Caption { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledLocal { get; set; }
}