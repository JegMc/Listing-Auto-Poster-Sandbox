namespace ListingAutoPosterSandbox.Web.Services;

public sealed class InstagramConnectionDiagnosticResult
{
    public bool IsConfigured { get; set; }

    public bool PageFound { get; set; }

    public string? PageId { get; set; }

    public string? PageName { get; set; }

    public bool InstagramAccountFound { get; set; }

    public string? InstagramAccountId { get; set; }

    public string? InstagramUsername { get; set; }

    public string? ExpectedInstagramUsername { get; set; }

    public bool? ExpectedInstagramUsernameMatches { get; set; }

    public string StatusMessage { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? RawResponseJson { get; set; }

    public bool InstagramSocialAccountSaved { get; set; }

    public string? InstagramSocialAccountSaveMessage { get; set; }

    public int? InstagramSocialAccountId { get; set; }
}