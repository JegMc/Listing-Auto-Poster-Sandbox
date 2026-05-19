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
    private readonly IScheduledPostPublisher _scheduledPostPublisher;

    public ListingsController(
        AppDbContext context,
        ICaptionGenerator captionGenerator,
        IScheduledPostPublisher scheduledPostPublisher)
    {
        _context = context;
        _captionGenerator = captionGenerator;
        _scheduledPostPublisher = scheduledPostPublisher;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var listings = await _context.Listings
            .OrderBy(listing => listing.Id)
            .ToListAsync(cancellationToken);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewAiFacebookPost(
        int id,
        CancellationToken cancellationToken)
    {
        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == id, cancellationToken);

        if (listing is null)
        {
            return NotFound("Listing not found.");
        }

        string caption;

        try
        {
            caption = await _captionGenerator.GenerateCaptionAsync(
                listing,
                cancellationToken);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"AI caption generation failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(caption))
        {
            TempData["Error"] = "AI caption generation returned an empty caption.";
            return RedirectToAction(nameof(Index));
        }

        var model = new FacebookPostReviewViewModel
        {
            ListingId = listing.Id,
            ListingTitle = listing.Title,
            ListingAddress = listing.Address,
            ListingDescription = listing.Description,
            ListingImageUrl = listing.ImageUrl,
            ListingPrice = listing.Price,
            Caption = caption.Trim()
        };

        return View("ReviewFacebookPost", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishReviewedFacebookPost(
        FacebookPostReviewViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await RehydrateListingReviewFieldsAsync(model, cancellationToken);
            return View("ReviewFacebookPost", model);
        }

        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == model.ListingId, cancellationToken);

        if (listing is null)
        {
            return NotFound("Listing not found.");
        }

        var facebookAccount = await _context.SocialAccounts
            .FirstOrDefaultAsync(
                account =>
                    account.Platform == PostPlatform.Facebook &&
                    account.IsConnected,
                cancellationToken);

        if (facebookAccount is null)
        {
            TempData["Error"] =
                "No connected Facebook account was found. Create or seed a connected Facebook SocialAccount before publishing.";

            return RedirectToAction(nameof(Index));
        }

        var nowUtc = DateTime.UtcNow;

        var scheduledPost = new ScheduledPost
        {
            ListingId = listing.Id,
            SocialAccountId = facebookAccount.Id,
            Platform = PostPlatform.Facebook,
            Caption = model.Caption.Trim(),
            ScheduledUtc = nowUtc,
            Status = PostStatus.Scheduled,
            CreatedUtc = nowUtc,
            UpdatedUtc = nowUtc
        };

        _context.ScheduledPosts.Add(scheduledPost);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _scheduledPostPublisher.PublishAsync(
                scheduledPost.Id,
                cancellationToken);

            TempData["Success"] =
                $"Reviewed Facebook post was published and saved as ScheduledPost {scheduledPost.Id}.";
        }
        catch (Exception ex)
        {
            TempData["Error"] =
                $"ScheduledPost {scheduledPost.Id} was created, but publishing failed: {ex.Message}";
        }

        return RedirectToAction(
            "Details",
            "ScheduledPosts",
            new { id = scheduledPost.Id });
    }

    private async Task RehydrateListingReviewFieldsAsync(
        FacebookPostReviewViewModel model,
        CancellationToken cancellationToken)
    {
        var listing = await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == model.ListingId, cancellationToken);

        if (listing is null)
        {
            return;
        }

        model.ListingTitle = listing.Title;
        model.ListingAddress = listing.Address;
        model.ListingDescription = listing.Description;
        model.ListingImageUrl = listing.ImageUrl;
        model.ListingPrice = listing.Price;
    }
}