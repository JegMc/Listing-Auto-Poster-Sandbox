namespace ListingAutoPosterSandbox.Web.Services;

public interface IDuePostScanner
{
    Task EnqueueDuePostsAsync();
}