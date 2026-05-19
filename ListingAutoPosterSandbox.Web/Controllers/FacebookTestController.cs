using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using ListingAutoPosterSandbox.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListingAutoPosterSandbox.Web.Controllers;

public sealed class FacebookTestController : Controller
{
    private readonly AppDbContext _context;
    private readonly IScheduledPostPublisher _scheduledPostPublisher;

    public FacebookTestController(
        AppDbContext context,
        IScheduledPostPublisher scheduledPostPublisher)
    {
        _context = context;
        _scheduledPostPublisher = scheduledPostPublisher;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new FacebookTestViewModel
        {
            Message = "YATCO BOSS scheduled-post pipeline test from the local ASP.NET app."
        };

        await PopulateOptionsAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostTest(
        FacebookTestViewModel model,
        CancellationToken cancellationToken)
    {
        await PopulateOptionsAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var listingExists = await _context.Listings
            .AnyAsync(listing => listing.Id == model.ListingId, cancellationToken);

        if (!listingExists)
        {
            ModelState.AddModelError(
                nameof(model.ListingId),
                "Selected listing was not found.");

            return View("Index", model);
        }

        var facebookAccount = await _context.SocialAccounts
            .FirstOrDefaultAsync(
                account =>
                    account.Id == model.FacebookAccountId &&
                    account.Platform == PostPlatform.Facebook &&
                    account.IsConnected,
                cancellationToken);

        if (facebookAccount is null)
        {
            ModelState.AddModelError(
                nameof(model.FacebookAccountId),
                "Connected Facebook account was not found.");

            return View("Index", model);
        }

        var nowUtc = DateTime.UtcNow;

        var scheduledPost = new ScheduledPost
        {
            ListingId = model.ListingId,
            SocialAccountId = facebookAccount.Id,
            Platform = PostPlatform.Facebook,
            Caption = model.Message.Trim(),
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
                $"ScheduledPost {scheduledPost.Id} was created and sent through the publisher.";
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

    private async Task PopulateOptionsAsync(
        FacebookTestViewModel model,
        CancellationToken cancellationToken)
    {
        model.Listings = await _context.Listings
            .OrderBy(listing => listing.Id)
            .Select(listing => new FacebookTestOption
            {
                Id = listing.Id,
                Label = $"{listing.Id} - {listing.Title}"
            })
            .ToListAsync(cancellationToken);

        model.FacebookAccounts = await _context.SocialAccounts
            .Where(account =>
                account.Platform == PostPlatform.Facebook &&
                account.IsConnected)
            .OrderBy(account => account.Id)
            .Select(account => new FacebookTestOption
            {
                Id = account.Id,
                Label = $"{account.Id} - {account.DisplayName}"
            })
            .ToListAsync(cancellationToken);

        if (model.ListingId == 0 && model.Listings.Count > 0)
        {
            model.ListingId = model.Listings[0].Id;
        }

        if (model.FacebookAccountId == 0 && model.FacebookAccounts.Count > 0)
        {
            model.FacebookAccountId = model.FacebookAccounts[0].Id;
        }
    }
}