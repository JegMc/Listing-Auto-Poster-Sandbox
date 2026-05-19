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

    [HttpGet]
    public IActionResult Connect()
    {
        var state = Guid.NewGuid().ToString("N");

        HttpContext.Session.SetString(OAuthStateSessionKey, state);

        var redirectUri = Url.Action(
            action: nameof(Callback),
            controller: "FacebookOAuth",
            values: null,
            protocol: Request.Scheme)!;

        var authorizationUrl = _facebookOAuthService.BuildAuthorizationUrl(
            redirectUri,
            state);

        return Redirect(authorizationUrl);
    }

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
            TempData["Error"] =
                $"Facebook connection failed: {error}. {error_description ?? error_reason}";

            return RedirectToAction("Index", "SocialAccounts");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["Error"] = "Facebook did not return an OAuth code.";
            return RedirectToAction("Index", "SocialAccounts");
        }

        var expectedState = HttpContext.Session.GetString(OAuthStateSessionKey);

        if (string.IsNullOrWhiteSpace(expectedState) ||
            string.IsNullOrWhiteSpace(state) ||
            expectedState != state)
        {
            TempData["Error"] = "Facebook OAuth state mismatch. Try connecting again.";
            return RedirectToAction("Index", "SocialAccounts");
        }

        var redirectUri = Url.Action(
            action: nameof(Callback),
            controller: "FacebookOAuth",
            values: null,
            protocol: Request.Scheme)!;

        var shortLivedToken =
            await _facebookOAuthService.ExchangeCodeForShortLivedTokenAsync(
                code,
                redirectUri,
                cancellationToken);

        var longLivedToken =
            await _facebookOAuthService.ExchangeForLongLivedTokenAsync(
                shortLivedToken.AccessToken,
                cancellationToken);

        var pages = await _facebookOAuthService.GetPagesAsync(
            longLivedToken.AccessToken,
            cancellationToken);

        if (pages.Count == 0)
        {
            TempData["Error"] =
                "Facebook connected, but no manageable Pages were returned. Confirm your Facebook user has full control of the test Page and granted Page access.";

            return RedirectToAction("Index", "SocialAccounts");
        }

        if (pages.Count == 1)
        {
            await SaveConnectedPageAsync(pages[0], cancellationToken);

            TempData["Success"] =
                $"Connected Facebook Page: {pages[0].Name}.";

            return RedirectToAction("Index", "SocialAccounts");
        }

        HttpContext.Session.SetString(
            PendingPagesSessionKey,
            JsonSerializer.Serialize(pages));

        return View("SelectPage", pages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPage(
        string pageId,
        CancellationToken cancellationToken)
    {
        var pagesJson = HttpContext.Session.GetString(PendingPagesSessionKey);

        if (string.IsNullOrWhiteSpace(pagesJson))
        {
            TempData["Error"] = "Facebook Page selection expired. Connect Facebook again.";
            return RedirectToAction("Index", "SocialAccounts");
        }

        var pages = JsonSerializer.Deserialize<List<FacebookPageAccount>>(pagesJson)
                    ?? new List<FacebookPageAccount>();

        var selectedPage = pages.FirstOrDefault(page => page.Id == pageId);

        if (selectedPage is null)
        {
            TempData["Error"] = "Selected Facebook Page was not found in the OAuth response.";
            return RedirectToAction("Index", "SocialAccounts");
        }

        await SaveConnectedPageAsync(selectedPage, cancellationToken);

        HttpContext.Session.Remove(PendingPagesSessionKey);

        TempData["Success"] =
            $"Connected Facebook Page: {selectedPage.Name}.";

        return RedirectToAction("Index", "SocialAccounts");
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

        var existingAccount = await _context.SocialAccounts
            .FirstOrDefaultAsync(
                account =>
                    account.Platform == PostPlatform.Facebook &&
                    account.PlatformAccountId == page.Id,
                cancellationToken);

        var nowUtc = DateTime.UtcNow;

        if (existingAccount is null)
        {
            existingAccount = await _context.SocialAccounts
                .FirstOrDefaultAsync(
                    account => account.Platform == PostPlatform.Facebook,
                    cancellationToken);
        }

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
}