namespace ListingAutoPosterSandbox.Web.Services.Development;

/// <summary>
/// Development-only fake implementation of ITokenStore.
/// 
/// This class does not read real Facebook tokens. It returns a fake token string
/// based on the requested secret name.
/// 
/// This class is intentionally not registered in Program.cs.
/// The active sandbox token path uses LocalFacebookTokenStore.
/// </summary>
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