using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using ListingAutoPosterSandbox.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListingAutoPosterSandbox.Web.Controllers;

public class ListingsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICaptionGenerator _captionGenerator;

    public ListingsController(
        AppDbContext context,
        ICaptionGenerator captionGenerator)
    {
        _context = context;
        _captionGenerator = captionGenerator;
    }

    public async Task<IActionResult> Index()
    {
        var listings = await _context.Listings
            .OrderBy(listing => listing.Id)
            .ToListAsync();

        return View(listings);
    }

    public async Task<IActionResult> GenerateCaption(
        int id,
        CancellationToken cancellationToken)
    {
        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == id, cancellationToken);

        if (listing is null)
        {
            return NotFound();
        }

        var caption = await _captionGenerator.GenerateCaptionAsync(
            listing,
            cancellationToken);

        var socialAccounts = await _context.SocialAccounts
            .Where(account => account.IsConnected)
            .OrderBy(account => account.Platform)
            .ThenBy(account => account.DisplayName)
            .ToListAsync(cancellationToken);

        var viewModel = new GeneratedCaptionViewModel
        {
            Listing = listing,
            Caption = caption,
            SocialAccounts = socialAccounts
        };

        return View(viewModel);
    }
}