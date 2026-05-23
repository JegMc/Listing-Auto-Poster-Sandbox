namespace ListingAutoPosterSandbox.Web.Services;

public sealed class InstagramDiagnosticOptions
{
    public string FacebookPageId { get; set; } = string.Empty;

    public string? ExpectedInstagramUsername { get; set; }

    public string GraphApiVersion { get; set; } = "v20.0";
}