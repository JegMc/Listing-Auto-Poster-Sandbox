# Listing Auto Poster Sandbox

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge)
![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-512BD4?style=for-the-badge)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-blue?style=for-the-badge)
![Meta Graph API](https://img.shields.io/badge/Meta%20Graph%20API-Facebook%20Pages-0866FF?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Sandbox%20POC-orange?style=for-the-badge)

A learning sandbox for building an AI-assisted social media auto-poster with ASP.NET Core MVC, Entity Framework Core, SQL Server LocalDB, Hangfire, OpenAI caption generation, and Facebook Page publishing through the Meta Graph API.

This project started as a fake social-posting pipeline. It now includes a working Facebook proof of concept:

- connect or represent a Facebook Page as a `SocialAccount`
- pick a sample listing
- generate an AI-written Facebook caption
- review and edit the caption before publishing
- create a durable `ScheduledPost` database row
- publish through the same scheduled-post pipeline used by the app
- send the final text to a real Facebook Page through the Meta Graph API
- save Meta's returned post ID and publish attempt details

This is still a sandbox, not a production-ready social media product. The purpose is to prove the architecture and learn the implementation path one phase at a time.

---

## Current Project Status

### Working now

- ASP.NET Core MVC dashboard
- sample listing cards
- OpenAI caption generation
- review/edit page before Facebook publishing
- `ScheduledPosts` database pipeline
- `PostAttempts` publish logging
- real Facebook Page text posting through the Meta Graph API
- `SocialAccount.PlatformAccountId` used as the Facebook Page ID
- local Facebook OAuth/token-store experiment
- Hangfire dashboard and recurring due-post scan flow
- details page showing post status, attempts, response JSON, and external post ID

### Still intentionally temporary

- local Facebook token storage is file-based for sandbox testing
- Facebook OAuth flow is still being hardened
- only Facebook text posting is implemented
- no Facebook image/photo posting yet
- no Instagram, LinkedIn, TikTok, or YouTube integration yet
- no user authentication or brokerage-level permissions
- no production secret storage
- Hangfire dashboard is local-development only and not production-secured
- retired fake/test code is kept under `docs/retired-code` for documentation history

---

## Core Workflow

```text
Broker opens Listings
        ↓
User clicks Draft Facebook Post
        ↓
App generates an AI caption with OpenAI
        ↓
User reviews and edits the Facebook post
        ↓
User clicks Publish Reviewed Post to Facebook
        ↓
App creates a ScheduledPost row
        ↓
ScheduledPostPublisher publishes through FacebookPagePoster
        ↓
Meta Graph API creates the Facebook Page post
        ↓
PostAttempt records the response
        ↓
ScheduledPost stores status, attempt count, response JSON, and external post ID
```

Project structure
```text
ListingAutoPosterSandbox
├── ListingAutoPosterSandbox.sln
├── README.md
├── .gitignore
├── docs
│   └── retired-code
│       ├── facebook-test-endpoint
│       └── fake-services
└── ListingAutoPosterSandbox.Web
    ├── Controllers
    │   ├── HomeController.cs
    │   ├── ListingsController.cs
    │   ├── ScheduledPostsController.cs
    │   ├── SocialAccountsController.cs
    │   └── FacebookOAuthController.cs
    ├── Data
    │   └── AppDbContext.cs
    ├── Migrations
    ├── Models
    │   ├── ErrorViewModel.cs
    │   ├── Listing.cs
    │   ├── ScheduledPost.cs
    │   ├── PostAttempt.cs
    │   ├── SocialAccount.cs
    │   ├── PostPlatform.cs
    │   ├── PostResult.cs
    │   └── PostStatus.cs
    ├── ViewModels
    │   ├── CreateScheduledPostViewModel.cs
    │   ├── FacebookPostReviewViewModel.cs
    │   └── GeneratedCaptionViewModel.cs
    ├── Services
    │   ├── ICaptionGenerator.cs
    │   ├── OpenAiCaptionGenerator.cs
    │   ├── IDuePostScanner.cs
    │   ├── DuePostScanner.cs
    │   ├── IScheduledPostPublisher.cs
    │   ├── ScheduledPostPublisher.cs
    │   ├── IPlatformPoster.cs
    │   ├── FacebookPagePoster.cs
    │   ├── FacebookPostResult.cs
    │   ├── FacebookOAuthService.cs
    │   ├── FacebookOAuthModels.cs
    │   ├── FacebookOptions.cs
    │   ├── ITokenStore.cs
    │   └── LocalFacebookTokenStore.cs
    ├── Views
    │   ├── Home
    │   ├── Listings
    │   ├── ScheduledPosts
    │   ├── SocialAccounts
    │   ├── FacebookOAuth
    │   └── Shared
    ├── wwwroot
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json
```
