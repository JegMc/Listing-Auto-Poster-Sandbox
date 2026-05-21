using System.ComponentModel.DataAnnotations;

namespace ListingAutoPosterSandbox.Web.ViewModels;

public class EditScheduledPostViewModel
{
    public int Id { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public string SocialAccountDisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Caption { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledLocal { get; set; }
}