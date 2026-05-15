using ListingAutoPosterSandbox.Web.Models;

namespace ListingAutoPosterSandbox.Web.Services;

public interface ICaptionGenerator
{
    Task<string> GenerateCaptionAsync(Listing listing, CancellationToken cancellationToken = default);
}