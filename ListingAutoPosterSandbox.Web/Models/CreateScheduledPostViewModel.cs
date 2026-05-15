using System.ComponentModel.DataAnnotations;

namespace ListingAutoPosterSandbox.Web.Models;

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