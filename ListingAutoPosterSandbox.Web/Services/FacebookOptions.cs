namespace ListingAutoPosterSandbox.Web.Services;

public sealed class FacebookOptions
{
    public string AppId { get; set; } = "";
    public string AppSecret { get; set; } = "";
    public string GraphApiVersion { get; set; } = "v25.0";
    public string TestPageId { get; set; } = "";
    public string TestPageAccessToken { get; set; } = "";
}