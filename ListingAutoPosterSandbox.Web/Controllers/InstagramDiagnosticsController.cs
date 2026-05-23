using ListingAutoPosterSandbox.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ListingAutoPosterSandbox.Web.Controllers;

public sealed class InstagramDiagnosticsController : Controller
{
    private readonly IInstagramConnectionDiagnosticService _diagnosticService;

    public InstagramDiagnosticsController(
        IInstagramConnectionDiagnosticService diagnosticService)
    {
        _diagnosticService = diagnosticService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _diagnosticService.CheckAsync(cancellationToken);
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDiscoveredAccount(
        CancellationToken cancellationToken)
    {
        var result = await _diagnosticService.CheckAndSaveAsync(cancellationToken);
        return View("Index", result);
    }
}