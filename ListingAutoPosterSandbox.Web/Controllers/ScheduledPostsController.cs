using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using ListingAutoPosterSandbox.Web.Services;
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

    public async Task<IActionResult> Index()
    {
        var scheduledPosts = await _context.ScheduledPosts
            .Include(post => post.Listing)
            .Include(post => post.SocialAccount)
            .OrderByDescending(post => post.CreatedUtc)
            .ToListAsync();

        return View(scheduledPosts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var scheduledPost = await _context.ScheduledPosts
            .Include(post => post.Listing)
            .Include(post => post.SocialAccount)
            .FirstOrDefaultAsync(post => post.Id == id);

        if (scheduledPost is null)
        {
            return NotFound();
        }

        var attempts = await _context.PostAttempts
            .Where(attempt => attempt.ScheduledPostId == id)
            .OrderByDescending(attempt => attempt.StartedUtc)
            .ToListAsync();

        ViewBag.Attempts = attempts;

        return View(scheduledPost);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateScheduledPostViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("The scheduled post form was invalid.");
        }

        var listingExists = await _context.Listings
    .AnyAsync(listing => listing.Id == viewModel.ListingId);

    if (!listingExists)
    {
        return NotFound("Listing not found.");
    }

    var socialAccount = await _context.SocialAccounts
        .FirstOrDefaultAsync(account =>
            account.Id == viewModel.SocialAccountId &&
            account.IsConnected);

    if (socialAccount is null)
    {
        return NotFound("Connected social account not found.");
    }

    var scheduledUtc = DateTime.SpecifyKind(
        viewModel.ScheduledLocal,
        DateTimeKind.Local).ToUniversalTime();

    var scheduledPost = new ScheduledPost
    {
        ListingId = viewModel.ListingId,
        SocialAccountId = socialAccount.Id,
        Platform = socialAccount.Platform,
        Caption = viewModel.Caption,
        ScheduledUtc = scheduledUtc,
        Status = PostStatus.Scheduled,
        CreatedUtc = DateTime.UtcNow,
        UpdatedUtc = DateTime.UtcNow
    };

        _context.ScheduledPosts.Add(scheduledPost);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishNow(int id, CancellationToken cancellationToken)
    {
        await _scheduledPostPublisher.PublishAsync(id, cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}