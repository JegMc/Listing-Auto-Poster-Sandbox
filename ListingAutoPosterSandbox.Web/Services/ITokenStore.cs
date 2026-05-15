namespace ListingAutoPosterSandbox.Web.Services;

public interface ITokenStore
{
    Task<string> GetAccessTokenAsync(
        string secretName,
        CancellationToken cancellationToken = default);
}