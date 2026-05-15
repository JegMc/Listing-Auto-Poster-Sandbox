namespace ListingAutoPosterSandbox.Web.Models;

public class Listing
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;
}