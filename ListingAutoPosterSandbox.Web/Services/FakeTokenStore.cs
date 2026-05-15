namespace ListingAutoPosterSandbox.Web.Services;

public class FakeTokenStore : ITokenStore
{
    public Task<string> GetAccessTokenAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new InvalidOperationException("Secret name is missing.");
        }

        var fakeToken = $"fake-token-for-{secretName}";

        return Task.FromResult(fakeToken);
    }
}