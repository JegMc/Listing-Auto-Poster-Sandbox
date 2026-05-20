using System.Text.Json;
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

            PostResult result;

            if (scheduledPost.Platform == PostPlatform.Facebook)
            {
                result = await PublishRealFacebookPostAsync(
                    scheduledPost,
                    cancellationToken);
            }
            else
            {
                result = CreateDemoPlatformResult(scheduledPost);
            }

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

            // Do not rethrow here.
            // In this sandbox, failed publishes should be recorded in the UI
            // instead of crashing the browser request.
        }
    }

    private async Task<PostResult> PublishRealFacebookPostAsync(
        ScheduledPost scheduledPost,
        CancellationToken cancellationToken)
    {
        if (scheduledPost.SocialAccount is null)
        {
            return new PostResult
            {
                Success = false,
                ErrorMessage = "Facebook post has no connected social account."
            };
        }

        var accessToken = await _tokenStore.GetAccessTokenAsync(
            scheduledPost.SocialAccount.SecretName,
            cancellationToken);

        return await _platformPoster.PublishAsync(
            scheduledPost,
            accessToken,
            cancellationToken);
    }

    private static PostResult CreateDemoPlatformResult(ScheduledPost scheduledPost)
    {
        var externalPostId = $"demo-{scheduledPost.Platform.ToString().ToLower()}-{scheduledPost.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var response = new
        {
            success = true,
            mode = "DemoOnly",
            message = "This platform is simulated in the sandbox. No external API call was made.",
            platform = scheduledPost.Platform.ToString(),
            scheduledPostId = scheduledPost.Id,
            externalPostId,
            publishedUtc = DateTime.UtcNow
        };

        return new PostResult
        {
            Success = true,
            ExternalPostId = externalPostId,
            ResponseJson = JsonSerializer.Serialize(response),
            ErrorMessage = null
        };
    }
}