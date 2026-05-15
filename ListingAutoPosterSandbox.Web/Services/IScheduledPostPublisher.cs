namespace ListingAutoPosterSandbox.Web.Services;

public interface IScheduledPostPublisher
{
    Task PublishAsync(int scheduledPostId, CancellationToken cancellationToken = default);
}