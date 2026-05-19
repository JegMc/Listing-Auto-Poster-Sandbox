using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using ListingAutoPosterSandbox.Web.Services;
using ListingAutoPosterSandbox.Web.ViewModels;
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

    // Shows the available sample listings.
    // From this page, the user can start either the older generic scheduling flow
    // or the newer Facebook-specific review/publish flow.
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var listings = await _context.Listings
            .OrderBy(listing => listing.Id)
            .ToListAsync(cancellationToken);

        return View(listings);
    }

    // Older generic scheduling flow.
    // This action generates a caption and sends the user to a page where they can schedule a post.
    // The newer Facebook-specific flow starts at ReviewAiFacebookPost and sends the user to a review/edit page first.
    [HttpGet]
    public async Task<IActionResult> GenerateCaption(
        int id,
        CancellationToken cancellationToken)
    {
        var listing = await GetListingAsync(id, cancellationToken);

        if (listing is null)
        {
            return NotFound();
        }

        var caption = await _captionGenerator.GenerateCaptionAsync(
            listing,
            cancellationToken);

        var socialAccounts = await GetConnectedSocialAccountsAsync(cancellationToken);

        var viewModel = new GeneratedCaptionViewModel
        {
            Listing = listing,
            Caption = caption,
            SocialAccounts = socialAccounts
        };

        return View(viewModel);
    }

    // Newer Facebook-specific flow.
    // This action creates an AI-generated draft and sends the user to a review/edit page.
    // It does not publish immediately.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewAiFacebookPost(
        int id,
        CancellationToken cancellationToken)
    {
        var listing = await GetListingAsync(id, cancellationToken);

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

        var model = BuildFacebookPostReviewViewModel(listing, caption);

        return View("ReviewFacebookPost", model);
    }

    // Publishes the user-reviewed Facebook post.
    // This creates a ScheduledPost row first, then sends that ScheduledPost through the normal publisher pipeline.
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

        var listing = await GetListingAsync(model.ListingId, cancellationToken);

        if (listing is null)
        {
            return NotFound("Listing not found.");
        }

        var facebookAccount = await GetConnectedFacebookAccountAsync(cancellationToken);

        if (facebookAccount is null)
        {
            TempData["Error"] = "No connected Facebook account was found. Create or seed a connected Facebook SocialAccount before publishing.";

            return RedirectToAction(nameof(Index));
        }

        var scheduledPost = CreateFacebookScheduledPost(
            listing,
            facebookAccount,
            model.Caption);

        _context.ScheduledPosts.Add(scheduledPost);

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _scheduledPostPublisher.PublishAsync(
                scheduledPost.Id,
                cancellationToken);

            TempData["Success"] = $"Reviewed Facebook post was published and saved as ScheduledPost {scheduledPost.Id}.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"ScheduledPost {scheduledPost.Id} was created, but publishing failed: {ex.Message}";
        }

        return RedirectToAction(
            "Details",
            "ScheduledPosts",
            new { id = scheduledPost.Id });
    }

    private async Task<Listing?> GetListingAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _context.Listings
            .FirstOrDefaultAsync(listing => listing.Id == id, cancellationToken);
    }

    private async Task<List<SocialAccount>> GetConnectedSocialAccountsAsync(
        CancellationToken cancellationToken)
    {
        return await _context.SocialAccounts
            .Where(account => account.IsConnected)
            .OrderBy(account => account.Platform)
            .ThenBy(account => account.DisplayName)
            .ToListAsync(cancellationToken);
    }

    private async Task<SocialAccount?> GetConnectedFacebookAccountAsync(
        CancellationToken cancellationToken)
    {
        return await _context.SocialAccounts
            .FirstOrDefaultAsync(
                account => account.Platform == PostPlatform.Facebook && account.IsConnected,
                cancellationToken);
    }

    private static FacebookPostReviewViewModel BuildFacebookPostReviewViewModel(
        Listing listing,
        string caption)
    {
        return new FacebookPostReviewViewModel
        {
            ListingId = listing.Id,
            ListingTitle = listing.Title,
            ListingAddress = listing.Address,
            ListingDescription = listing.Description,
            ListingImageUrl = listing.ImageUrl,
            ListingPrice = listing.Price,
            Caption = caption.Trim()
        };
    }

    private static ScheduledPost CreateFacebookScheduledPost(
        Listing listing,
        SocialAccount facebookAccount,
        string caption)
    {
        var nowUtc = DateTime.UtcNow;

        return new ScheduledPost
        {
            ListingId = listing.Id,
            SocialAccountId = facebookAccount.Id,
            Platform = PostPlatform.Facebook,
            Caption = caption.Trim(),
            ScheduledUtc = nowUtc,
            Status = PostStatus.Scheduled,
            CreatedUtc = nowUtc,
            UpdatedUtc = nowUtc
        };
    }

    private async Task RehydrateListingReviewFieldsAsync(
        FacebookPostReviewViewModel model,
        CancellationToken cancellationToken)
    {
        var listing = await GetListingAsync(model.ListingId, cancellationToken);

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