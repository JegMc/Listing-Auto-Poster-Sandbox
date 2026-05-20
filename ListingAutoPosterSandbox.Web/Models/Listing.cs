namespace ListingAutoPosterSandbox.Web.Models;

public class Listing
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    // Kept for compatibility with the earlier real-estate version.
    // In the UI, this should be treated as a fallback Location.
    public string Address { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Builder { get; set; } = string.Empty;

    public string BrokerageCompany { get; set; } = string.Empty;

    public decimal? LengthFeet { get; set; }

    public int? YearBuilt { get; set; }

    public int? Cabins { get; set; }

    public int? Guests { get; set; }

    public decimal? MaxSpeedKnots { get; set; }

    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;
}