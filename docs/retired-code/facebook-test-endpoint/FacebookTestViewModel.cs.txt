using System.ComponentModel.DataAnnotations;

namespace ListingAutoPosterSandbox.Web.Models;

public sealed class FacebookTestViewModel
{
    [Required]
    [Display(Name = "Listing")]
    public int ListingId { get; set; }

    [Required]
    [Display(Name = "Facebook account")]
    public int FacebookAccountId { get; set; }

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    [Display(Name = "Facebook post message")]
    public string Message { get; set; } = "";

    public List<FacebookTestOption> Listings { get; set; } = new();

    public List<FacebookTestOption> FacebookAccounts { get; set; } = new();
}

public sealed class FacebookTestOption
{
    public int Id { get; set; }

    public string Label { get; set; } = "";
}