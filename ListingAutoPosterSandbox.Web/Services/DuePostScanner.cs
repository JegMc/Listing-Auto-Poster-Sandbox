using Hangfire;
using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ListingAutoPosterSandbox.Web.Services;

public class DuePostScanner : IDuePostScanner
{
    private readonly AppDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public DuePostScanner(
        AppDbContext context,
        IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task EnqueueDuePostsAsync()
    {
        var nowUtc = DateTime.UtcNow;

        var duePosts = await _context.ScheduledPosts
            .Where(post =>
                post.Status == PostStatus.Scheduled &&
                post.ScheduledUtc <= nowUtc)
            .OrderBy(post => post.ScheduledUtc)
            .ToListAsync();

        foreach (var post in duePosts)
        {
            post.Status = PostStatus.Processing;
            post.UpdatedUtc = DateTime.UtcNow;

            _backgroundJobClient.Enqueue<IScheduledPostPublisher>(
                publisher => publisher.PublishAsync(post.Id, CancellationToken.None));
        }

        await _context.SaveChangesAsync();
    }
}