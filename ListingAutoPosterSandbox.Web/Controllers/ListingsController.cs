using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using ListingAutoPosterSandbox.Web.Services;
using ListingAutoPosterSandbox.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListingAutoPosterSandbox.Web.Controllers;

public class ListingsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICaptionGenerator _captionGenerator;
    private readonly IScheduledPostPublisher _scheduledPostPublisher;
    private readonly IWebHostEnvironment _environment;

    public ListingsController(
        AppDbContext context,
        ICaptionGenerator captionGenerator,
        IScheduledPostPublisher scheduledPostPublisher,
        IWebHostEnvironment environment)
    {
        _context = context;
        _captionGenerator = captionGenerator;
        _scheduledPostPublisher = scheduledPostPublisher;
        _environment = environment;
    }

    // Shows the available sample listings.
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var listings = await _context.Listings
            .OrderBy(listing => listing.Id)
            .ToListAsync(cancellationToken);

        return View(listings);
    }

    // Generic scheduling flow.
    // This action generates a caption and sends the user to a page where they can schedule one or more posts.
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateCustomCaption(
        CustomListingInputViewModel input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter custom yacht or marketing information before generating a caption.";
            return RedirectToAction(nameof(Index));
        }

        string? uploadedImageUrl;

        try
        {
            uploadedImageUrl = await SaveUploadedHeroImageAsync(
                input.HeroImageFile,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        var finalImageUrl = uploadedImageUrl
            ?? (!string.IsNullOrWhiteSpace(input.ImageUrl)
                ? input.ImageUrl.Trim()
                : "https://placehold.co/600x400?text=Custom+Yacht");

        var location = input.Location?.Trim() ?? string.Empty;

        var listing = new Listing
        {
            Title = string.IsNullOrWhiteSpace(input.Title)
                ? "Custom Yacht Post"
                : input.Title.Trim(),

            Builder = input.Builder?.Trim() ?? string.Empty,
            BrokerageCompany = input.BrokerageCompany?.Trim() ?? string.Empty,
            LengthFeet = input.LengthFeet,
            YearBuilt = input.YearBuilt,
            Location = location,
            Address = location,
            Price = input.Price ?? 0,
            Cabins = input.Cabins,
            Guests = input.Guests,
            MaxSpeedKnots = input.MaxSpeedKnots,

            // The freeform user input becomes the main description sent to the AI caption generator.
            Description = input.CustomDetails.Trim(),

            // Can now be either:
            // 1. a public image URL, or
            // 2. a local app upload path like /uploads/listings/example.jpg
            ImageUrl = finalImageUrl
        };

        _context.Listings.Add(listing);
        await _context.SaveChangesAsync(cancellationToken);

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

        return View("GenerateCaption", viewModel);
    }

    // Older Facebook-specific review flow.
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

    private async Task<string?> SaveUploadedHeroImageAsync(
        IFormFile? imageFile,
        CancellationToken cancellationToken)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return null;
        }

        const long maxBytes = 5 * 1024 * 1024;

        if (imageFile.Length > maxBytes)
        {
            throw new InvalidOperationException("Hero image must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        var allowedExtensions = new HashSet<string>
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Hero image must be a JPG, PNG, or WebP file.");
        }

        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadsFolder = Path.Combine(
            webRootPath,
            "uploads",
            "listings");

        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsFolder, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await imageFile.CopyToAsync(stream, cancellationToken);

        return $"/uploads/listings/{fileName}";
    }
}