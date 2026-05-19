using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using ListingAutoPosterSandbox.Web.Services;
using ListingAutoPosterSandbox.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListingAutoPosterSandbox.Web.Controllers;

public class ScheduledPostsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IScheduledPostPublisher _scheduledPostPublisher;

    public ScheduledPostsController(
        AppDbContext context,
        IScheduledPostPublisher scheduledPostPublisher)
    {
        _context = context;
        _scheduledPostPublisher = scheduledPostPublisher;
    }

    // Shows all scheduled posts, newest first.
    // Includes Listing and SocialAccount so the Razor view can display useful related information.
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var scheduledPosts = await _context.ScheduledPosts
            .Include(post => post.Listing)
            .Include(post => post.SocialAccount)
            .OrderByDescending(post => post.CreatedUtc)
            .ToListAsync(cancellationToken);

        return View(scheduledPosts);
    }

    // Shows one scheduled post and its publish attempts.
    // PostAttempt records are useful for debugging whether a publish succeeded or failed.
    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var scheduledPost = await _context.ScheduledPosts
            .Include(post => post.Listing)
            .Include(post => post.SocialAccount)
            .FirstOrDefaultAsync(post => post.Id == id, cancellationToken);

        if (scheduledPost is null)
        {
            return NotFound();
        }

        var attempts = await _context.PostAttempts
            .Where(attempt => attempt.ScheduledPostId == id)
            .OrderByDescending(attempt => attempt.StartedUtc)
            .ToListAsync(cancellationToken);

        ViewBag.Attempts = attempts;

        return View(scheduledPost);
    }

    // Handles the older generic scheduling form from Listings/GenerateCaption.
    // The newer reviewed Facebook flow creates and publishes ScheduledPost records from ListingsController.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateScheduledPostViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("The scheduled post form was invalid.");
        }

        var listingExists = await _context.Listings
            .AnyAsync(listing => listing.Id == viewModel.ListingId, cancellationToken);

        if (!listingExists)
        {
            return NotFound("Listing not found.");
        }

        var socialAccount = await _context.SocialAccounts
            .FirstOrDefaultAsync(
                account => account.Id == viewModel.SocialAccountId && account.IsConnected,
                cancellationToken);

        if (socialAccount is null)
        {
            return NotFound("Connected social account not found.");
        }

        var scheduledUtc = DateTime.SpecifyKind(
                viewModel.ScheduledLocal,
                DateTimeKind.Local)
            .ToUniversalTime();

        var nowUtc = DateTime.UtcNow;

        var scheduledPost = new ScheduledPost
        {
            ListingId = viewModel.ListingId,
            SocialAccountId = socialAccount.Id,
            Platform = socialAccount.Platform,
            Caption = viewModel.Caption.Trim(),
            ScheduledUtc = scheduledUtc,
            Status = PostStatus.Scheduled,
            CreatedUtc = nowUtc,
            UpdatedUtc = nowUtc
        };

        _context.ScheduledPosts.Add(scheduledPost);

        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    // Manually publishes an existing ScheduledPost immediately.
    // This still goes through ScheduledPostPublisher, so publish attempts are recorded normally.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishNow(
        int id,
        CancellationToken cancellationToken)
    {
        await _scheduledPostPublisher.PublishAsync(id, cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}