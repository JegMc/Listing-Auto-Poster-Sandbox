using Microsoft.Extensions.Options;

namespace ListingAutoPosterSandbox.Web.Services;

public sealed class UserSecretsFacebookTokenStore : ITokenStore
{
    private readonly FacebookOptions _options;

    public UserSecretsFacebookTokenStore(IOptions<FacebookOptions> options)
    {
        _options = options.Value;
    }

    public Task<string> GetAccessTokenAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.TestPageAccessToken))
        {
            throw new InvalidOperationException(
                "Missing Facebook:TestPageAccessToken. Add a fresh Page access token with dotnet user-secrets.");
        }

        return Task.FromResult(_options.TestPageAccessToken);
    }
}