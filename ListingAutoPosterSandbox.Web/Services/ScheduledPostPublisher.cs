using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ListingAutoPosterSandbox.Web.Services;

public class ScheduledPostPublisher : IScheduledPostPublisher
{
    private readonly AppDbContext _context;
    private readonly IPlatformPoster _platformPoster;
    private readonly ITokenStore _tokenStore;

    public ScheduledPostPublisher(
        AppDbContext context,
        IPlatformPoster platformPoster,
        ITokenStore tokenStore)
    {
        _context = context;
        _platformPoster = platformPoster;
        _tokenStore = tokenStore;
    }

    public async Task PublishAsync(
        int scheduledPostId,
        CancellationToken cancellationToken = default)
    {
        var scheduledPost = await _context.ScheduledPosts
            .Include(post => post.Listing)
            .Include(post => post.SocialAccount)
            .FirstOrDefaultAsync(
                post => post.Id == scheduledPostId,
                cancellationToken);

        if (scheduledPost is null)
        {
            throw new InvalidOperationException($"Scheduled post {scheduledPostId} was not found.");
        }

        if (scheduledPost.Status == PostStatus.Posted)
        {
            return;
        }

        if (scheduledPost.Status != PostStatus.Processing)
        {
            scheduledPost.Status = PostStatus.Processing;
        }

        scheduledPost.AttemptCount += 1;
        scheduledPost.UpdatedUtc = DateTime.UtcNow;

        var attempt = new PostAttempt
        {
            ScheduledPostId = scheduledPost.Id,
            StartedUtc = DateTime.UtcNow,
            Success = false
        };

        _context.PostAttempts.Add(attempt);

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            if (scheduledPost.SocialAccount is null)
            {
                throw new InvalidOperationException(
                    $"Scheduled post {scheduledPost.Id} does not have a social account.");
            }

            var accessToken = await _tokenStore.GetAccessTokenAsync(
                scheduledPost.SocialAccount.SecretName,
                cancellationToken);

            var result = await _platformPoster.PublishAsync(
                scheduledPost,
                accessToken,
                cancellationToken);

            attempt.CompletedUtc = DateTime.UtcNow;
            attempt.Success = result.Success;
            attempt.ResponseJson = result.ResponseJson;
            attempt.ErrorMessage = result.ErrorMessage;

            if (result.Success)
            {
                scheduledPost.Status = PostStatus.Posted;
                scheduledPost.ExternalPostId = result.ExternalPostId;
                scheduledPost.LastError = null;
            }
            else
            {
                scheduledPost.Status = PostStatus.Failed;
                scheduledPost.LastError = result.ErrorMessage ?? "Publishing failed.";
            }

            scheduledPost.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            attempt.CompletedUtc = DateTime.UtcNow;
            attempt.Success = false;
            attempt.ErrorMessage = ex.Message;

            scheduledPost.Status = PostStatus.Failed;
            scheduledPost.LastError = ex.Message;
            scheduledPost.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            throw;
        }
    }
}