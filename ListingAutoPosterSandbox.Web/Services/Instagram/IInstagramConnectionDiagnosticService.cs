namespace ListingAutoPosterSandbox.Web.Services;

public interface IInstagramConnectionDiagnosticService
{
    Task<InstagramConnectionDiagnosticResult> CheckAsync(
        CancellationToken cancellationToken = default);

    Task<InstagramConnectionDiagnosticResult> CheckAndSaveAsync(
        CancellationToken cancellationToken = default);
}