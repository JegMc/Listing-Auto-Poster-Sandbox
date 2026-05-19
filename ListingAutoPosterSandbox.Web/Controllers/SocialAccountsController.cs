using ListingAutoPosterSandbox.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListingAutoPosterSandbox.Web.Controllers;

public sealed class SocialAccountsController : Controller
{
    private readonly AppDbContext _context;

    public SocialAccountsController(AppDbContext context)
    {
        _context = context;
    }

    // Shows the connected social accounts that the sandbox can publish through.
    // For the current Facebook flow, this page is also the starting point for Facebook OAuth.
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accounts = await _context.SocialAccounts
            .AsNoTracking()
            .OrderBy(account => account.Platform)
            .ThenBy(account => account.DisplayName)
            .ToListAsync(cancellationToken);

        return View(accounts);
    }
}