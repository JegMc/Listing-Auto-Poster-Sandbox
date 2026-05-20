using System.ComponentModel.DataAnnotations;

namespace ListingAutoPosterSandbox.Web.ViewModels;

/// <summary>
/// Flexible form input for the custom yacht card on the Listings page.
/// This is intentionally loose because the user may want to paste any custom yacht,
/// brokerage, buyer, seller, or marketing information into one box.
/// </summary>
public class CustomListingInputViewModel
{
    [StringLength(120)]
    public string? Title { get; set; }

    [StringLength(120)]
    public string? Builder { get; set; }

    [StringLength(120)]
    public string? BrokerageCompany { get; set; }

    [StringLength(120)]
    public string? Location { get; set; }

    [Range(0, 1000000000)]
    public decimal? Price { get; set; }

    [Range(1, 1000)]
    public decimal? LengthFeet { get; set; }

    [Range(1800, 2100)]
    public int? YearBuilt { get; set; }

    [Range(0, 100)]
    public int? Cabins { get; set; }

    [Range(0, 500)]
    public int? Guests { get; set; }

    [Range(0, 200)]
    public decimal? MaxSpeedKnots { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Enter some custom yacht or marketing information.")]
    [StringLength(4000)]
    public string CustomDetails { get; set; } = string.Empty;
}