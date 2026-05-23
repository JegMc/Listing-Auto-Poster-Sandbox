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
            TempData["Error"] = "The scheduled post form was invalid. Select at least one social account and try again.";
            return RedirectToAction("Index", "Listings");
        }

        var listing = await _context.Listings
            .FirstOrDefaultAsync(
                listing => listing.Id == viewModel.ListingId,
                cancellationToken);

        if (listing is null)
        {
            return NotFound("Listing not found.");
        }

        var selectedAccountIds = viewModel.SocialAccountIds
            .Distinct()
            .ToList();

        var socialAccounts = await _context.SocialAccounts
            .Where(account => selectedAccountIds.Contains(account.Id) && account.IsConnected)
            .OrderBy(account => account.Platform)
            .ToListAsync(cancellationToken);

        if (socialAccounts.Count == 0)
        {
            TempData["Error"] = "No connected social accounts were selected.";
            return RedirectToAction("Index", "Listings");
        }

        var scheduledUtc = DateTime.SpecifyKind(
                viewModel.ScheduledLocal,
                DateTimeKind.Local)
            .ToUniversalTime();

        var nowUtc = DateTime.UtcNow;

        foreach (var socialAccount in socialAccounts)
        {
            var scheduledPost = new ScheduledPost
            {
                ListingId = viewModel.ListingId,
                SocialAccountId = socialAccount.Id,
                Platform = socialAccount.Platform,
                Caption = viewModel.Caption.Trim(),

                // Only attach the listing hero image if the user explicitly checked the image option.
                ImageUrl = viewModel.IncludeImage && !string.IsNullOrWhiteSpace(listing.ImageUrl)
                    ? listing.ImageUrl
                    : null,

                ScheduledUtc = scheduledUtc,
                Status = PostStatus.Scheduled,
                CreatedUtc = nowUtc,
                UpdatedUtc = nowUtc
            };

            _context.ScheduledPosts.Add(scheduledPost);
        }

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Created {socialAccounts.Count} scheduled post(s).";
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


    [HttpGet]
    public async Task<IActionResult> Edit(
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

        if (!CanEditPost(scheduledPost.Status))
        {
            TempData["Error"] = "Only scheduled or failed posts can be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var viewModel = new EditScheduledPostViewModel
        {
            Id = scheduledPost.Id,
            ListingTitle = scheduledPost.Listing?.Title ?? "Unknown listing",
            Platform = scheduledPost.Platform.ToString(),
            SocialAccountDisplayName = scheduledPost.SocialAccount?.DisplayName ?? "None",
            Status = scheduledPost.Status.ToString(),
            Caption = scheduledPost.Caption,
            ImageUrl = scheduledPost.ImageUrl,
            ScheduledLocal = DateTime.SpecifyKind(
                scheduledPost.ScheduledUtc,
                DateTimeKind.Utc)
                .ToLocalTime()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditScheduledPostViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var scheduledPost = await _context.ScheduledPosts
            .Include(post => post.Listing)
            .Include(post => post.SocialAccount)
            .FirstOrDefaultAsync(post => post.Id == viewModel.Id, cancellationToken);

        if (scheduledPost is null)
        {
            return NotFound();
        }

        if (!CanEditPost(scheduledPost.Status))
        {
            TempData["Error"] = "Only scheduled or failed posts can be edited.";
            return RedirectToAction(nameof(Details), new { id = scheduledPost.Id });
        }

        if (!ModelState.IsValid)
        {
            viewModel.ListingTitle = scheduledPost.Listing?.Title ?? "Unknown listing";
            viewModel.Platform = scheduledPost.Platform.ToString();
            viewModel.SocialAccountDisplayName = scheduledPost.SocialAccount?.DisplayName ?? "None";
            viewModel.Status = scheduledPost.Status.ToString();

            return View(viewModel);
        }

        var scheduledUtc = DateTime.SpecifyKind(
                viewModel.ScheduledLocal,
                DateTimeKind.Local)
            .ToUniversalTime();

        if (scheduledUtc <= DateTime.UtcNow)
        {
            ModelState.AddModelError(
                nameof(viewModel.ScheduledLocal),
                "Scheduled time must be in the future.");

            viewModel.ListingTitle = scheduledPost.Listing?.Title ?? "Unknown listing";
            viewModel.Platform = scheduledPost.Platform.ToString();
            viewModel.SocialAccountDisplayName = scheduledPost.SocialAccount?.DisplayName ?? "None";
            viewModel.Status = scheduledPost.Status.ToString();

            return View(viewModel);
        }

        scheduledPost.Caption = viewModel.Caption.Trim();
        scheduledPost.ImageUrl = string.IsNullOrWhiteSpace(viewModel.ImageUrl)
            ? null
            : viewModel.ImageUrl.Trim();
        scheduledPost.ScheduledUtc = scheduledUtc;

        // If this post failed before, editing it means we are preparing it for another attempt.
        if (scheduledPost.Status == PostStatus.Failed)
        {
            scheduledPost.Status = PostStatus.Scheduled;
            scheduledPost.LastError = null;
            scheduledPost.ExternalPostId = null;
        }

        scheduledPost.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Scheduled post updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        var scheduledPost = await _context.ScheduledPosts
            .FirstOrDefaultAsync(post => post.Id == id, cancellationToken);

        if (scheduledPost is null)
        {
            return NotFound();
        }

        if (!CanCancelPost(scheduledPost.Status))
        {
            TempData["Error"] = "Only scheduled or failed posts can be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        scheduledPost.Status = PostStatus.Cancelled;
        scheduledPost.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Post cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        int id,
        CancellationToken cancellationToken)
    {
        var scheduledPost = await _context.ScheduledPosts
            .FirstOrDefaultAsync(post => post.Id == id, cancellationToken);

        if (scheduledPost is null)
        {
            return NotFound();
        }

        if (!CanRemovePost(scheduledPost.Status))
        {
            TempData["Error"] = "Only failed or cancelled posts can be removed from the sandbox queue.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var attempts = await _context.PostAttempts
            .Where(attempt => attempt.ScheduledPostId == id)
            .ToListAsync(cancellationToken);

        _context.PostAttempts.RemoveRange(attempts);
        _context.ScheduledPosts.Remove(scheduledPost);

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Post removed from the sandbox queue.";
        return RedirectToAction(nameof(Index));
    }

    private static bool CanEditPost(PostStatus status)
    {
        return status == PostStatus.Scheduled ||
            status == PostStatus.Failed;
    }

    private static bool CanCancelPost(PostStatus status)
    {
        return status == PostStatus.Scheduled ||
            status == PostStatus.Failed;
    }

    private static bool CanRemovePost(PostStatus status)
    {
        return status == PostStatus.Failed ||
            status == PostStatus.Cancelled;
    }
}