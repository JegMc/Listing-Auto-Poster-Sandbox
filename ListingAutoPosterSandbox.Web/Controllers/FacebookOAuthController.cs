using System.Text.Json;
using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Models;
using ListingAutoPosterSandbox.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListingAutoPosterSandbox.Web.Controllers;

public sealed class FacebookOAuthController : Controller
{
    private const string OAuthStateSessionKey = "facebook_oauth_state";
    private const string PendingPagesSessionKey = "facebook_pending_pages";

    private readonly AppDbContext _context;
    private readonly FacebookOAuthService _facebookOAuthService;
    private readonly LocalFacebookTokenStore _tokenStore;

    public FacebookOAuthController(
        AppDbContext context,
        FacebookOAuthService facebookOAuthService,
        LocalFacebookTokenStore tokenStore)
    {
        _context = context;
        _facebookOAuthService = facebookOAuthService;
        _tokenStore = tokenStore;
    }

    // Starts the Facebook OAuth connection flow.
    // This sends the user to Facebook so they can grant Page access to the sandbox app.
    [HttpGet]
    public IActionResult Connect()
    {
        var state = CreateAndStoreOAuthState();
        var redirectUri = BuildCallbackRedirectUri();

        var authorizationUrl = _facebookOAuthService.BuildAuthorizationUrl(
            redirectUri,
            state);

        return Redirect(authorizationUrl);
    }

    // Handles Facebook's redirect after the user approves or rejects the OAuth request.
    // This action validates the OAuth response, gets Page access tokens, and either saves the only Page
    // or asks the user to choose which Page the sandbox should publish to.
    [HttpGet]
    public async Task<IActionResult> Callback(
        string? code,
        string? state,
        string? error,
        string? error_reason,
        string? error_description,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            TempData["Error"] = BuildFacebookErrorMessage(
                error,
                error_reason,
                error_description);

            return RedirectToSocialAccountsIndex();
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["Error"] = "Facebook did not return an OAuth code.";

            return RedirectToSocialAccountsIndex();
        }

        if (!IsValidOAuthState(state))
        {
            TempData["Error"] = "Facebook OAuth state mismatch. Try connecting again.";

            return RedirectToSocialAccountsIndex();
        }

        try
        {
            var pages = await GetManageableFacebookPagesAsync(
                code,
                cancellationToken);

            if (pages.Count == 0)
            {
                TempData["Error"] = "Facebook connected, but no manageable Pages were returned. Confirm your Facebook user has full control of the test Page and granted Page access.";

                return RedirectToSocialAccountsIndex();
            }

            if (pages.Count == 1)
            {
                await SaveConnectedPageAsync(
                    pages[0],
                    cancellationToken);

                TempData["Success"] = $"Connected Facebook Page: {pages[0].Name}.";

                return RedirectToSocialAccountsIndex();
            }

            StorePendingPages(pages);

            return View("SelectPage", pages);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Facebook connection failed: {ex.Message}";

            return RedirectToSocialAccountsIndex();
        }
    }

    // Handles the case where the Facebook user manages more than one Page.
    // The selected Page's access token is saved locally, and the app records the Page as a SocialAccount.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPage(
        string pageId,
        CancellationToken cancellationToken)
    {
        var pages = GetPendingPagesFromSession();

        if (pages is null)
        {
            TempData["Error"] = "Facebook Page selection expired. Connect Facebook again.";

            return RedirectToSocialAccountsIndex();
        }

        var selectedPage = pages.FirstOrDefault(page => page.Id == pageId);

        if (selectedPage is null)
        {
            TempData["Error"] = "Selected Facebook Page was not found in the OAuth response.";

            return RedirectToSocialAccountsIndex();
        }

        await SaveConnectedPageAsync(
            selectedPage,
            cancellationToken);

        ClearPendingPages();

        TempData["Success"] = $"Connected Facebook Page: {selectedPage.Name}.";

        return RedirectToSocialAccountsIndex();
    }

    private string CreateAndStoreOAuthState()
    {
        var state = Guid.NewGuid().ToString("N");

        HttpContext.Session.SetString(
            OAuthStateSessionKey,
            state);

        return state;
    }

    private bool IsValidOAuthState(string? returnedState)
    {
        var expectedState = HttpContext.Session.GetString(OAuthStateSessionKey);

        HttpContext.Session.Remove(OAuthStateSessionKey);

        return !string.IsNullOrWhiteSpace(expectedState)
            && !string.IsNullOrWhiteSpace(returnedState)
            && expectedState == returnedState;
    }

    private string BuildCallbackRedirectUri()
    {
        return Url.Action(
            action: nameof(Callback),
            controller: "FacebookOAuth",
            values: null,
            protocol: Request.Scheme)!;
    }

    private async Task<List<FacebookPageAccount>> GetManageableFacebookPagesAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var redirectUri = BuildCallbackRedirectUri();

        var shortLivedToken = await _facebookOAuthService.ExchangeCodeForShortLivedTokenAsync(
            code,
            redirectUri,
            cancellationToken);

        var longLivedToken = await _facebookOAuthService.ExchangeForLongLivedTokenAsync(
            shortLivedToken.AccessToken,
            cancellationToken);

        return await _facebookOAuthService.GetPagesAsync(
            longLivedToken.AccessToken,
            cancellationToken);
    }

    private void StorePendingPages(List<FacebookPageAccount> pages)
    {
        HttpContext.Session.SetString(
            PendingPagesSessionKey,
            JsonSerializer.Serialize(pages));
    }

    private List<FacebookPageAccount>? GetPendingPagesFromSession()
    {
        var pagesJson = HttpContext.Session.GetString(PendingPagesSessionKey);

        if (string.IsNullOrWhiteSpace(pagesJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<FacebookPageAccount>>(pagesJson)
            ?? new List<FacebookPageAccount>();
    }

    private void ClearPendingPages()
    {
        HttpContext.Session.Remove(PendingPagesSessionKey);
    }

    private async Task SaveConnectedPageAsync(
        FacebookPageAccount page,
        CancellationToken cancellationToken)
    {
        var secretName = $"local/facebook/page/{page.Id}";

        await _tokenStore.SaveAccessTokenAsync(
            secretName,
            page.AccessToken,
            cancellationToken);

        var existingAccount = await FindExistingFacebookAccountAsync(
            page.Id,
            cancellationToken);

        var nowUtc = DateTime.UtcNow;

        if (existingAccount is null)
        {
            _context.SocialAccounts.Add(new SocialAccount
            {
                Platform = PostPlatform.Facebook,
                DisplayName = page.Name,
                SecretName = secretName,
                PlatformAccountId = page.Id,
                IsConnected = true,
                CreatedUtc = nowUtc,
                UpdatedUtc = nowUtc
            });
        }
        else
        {
            existingAccount.DisplayName = page.Name;
            existingAccount.SecretName = secretName;
            existingAccount.PlatformAccountId = page.Id;
            existingAccount.IsConnected = true;
            existingAccount.UpdatedUtc = nowUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<SocialAccount?> FindExistingFacebookAccountAsync(
        string pageId,
        CancellationToken cancellationToken)
    {
        var accountForSamePage = await _context.SocialAccounts
            .FirstOrDefaultAsync(
                account => account.Platform == PostPlatform.Facebook
                    && account.PlatformAccountId == pageId,
                cancellationToken);

        if (accountForSamePage is not null)
        {
            return accountForSamePage;
        }

        // Fallback for earlier sandbox records that may not have had PlatformAccountId yet.
        return await _context.SocialAccounts
            .FirstOrDefaultAsync(
                account => account.Platform == PostPlatform.Facebook,
                cancellationToken);
    }

    private IActionResult RedirectToSocialAccountsIndex()
    {
        return RedirectToAction(
            "Index",
            "SocialAccounts");
    }

    private static string BuildFacebookErrorMessage(
        string error,
        string? errorReason,
        string? errorDescription)
    {
        var details = errorDescription ?? errorReason;

        if (string.IsNullOrWhiteSpace(details))
        {
            return $"Facebook connection failed: {error}.";
        }

        return $"Facebook connection failed: {error}. {details}";
    }
}