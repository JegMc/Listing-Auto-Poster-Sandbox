# Listing Auto Poster Sandbox

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge)
![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-2C7A7B?style=for-the-badge)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-orange?style=for-the-badge)
![Meta Graph API](https://img.shields.io/badge/Meta%20Graph%20API-Facebook%20Posting-1877F2?style=for-the-badge&logo=facebook&logoColor=white)
![Status](https://img.shields.io/badge/Status-Active%20Sandbox-brightgreen?style=for-the-badge)

A learning sandbox for building an AI-assisted social media auto-poster with **ASP.NET Core MVC**, **Entity Framework Core**, **SQL Server LocalDB**, **Hangfire**, **OpenAI caption generation**, and **Facebook Page publishing through the Meta Graph API**.

This project started as a fake social-posting pipeline. It now includes a working Facebook proof of concept where a user can select a listing, generate an AI-written Facebook caption, review/edit the post text, publish to a real Facebook Page, and inspect the stored publish result.

This is still a sandbox, not a production-ready social media platform.

---

## Current Status

### Working now

- ASP.NET Core MVC web app
- SQL Server LocalDB persistence through EF Core
- Sample listing cards
- OpenAI-generated listing captions
- Facebook-specific review/edit page before publishing
- `ScheduledPost` database pipeline
- `PostAttempt` publish logging
- Real Facebook Page text posting through the Meta Graph API
- `SocialAccount.PlatformAccountId` stored as the Facebook Page ID
- Local Facebook OAuth/token-store experiment
- Hangfire dashboard
- Recurring Hangfire scan for due scheduled posts
- Scheduled post details page showing:
  - current status
  - related listing
  - related social account
  - publish attempts
  - response JSON
  - external Facebook post ID

### Recently cleaned up

- Reorganized services so Facebook-specific code is easier to find.
- Moved fake services into a development-only area.
- Retired the old manual Facebook test endpoint into `docs/retired-code`.
- Moved ViewModels out of `Models` and into a dedicated `ViewModels` folder.
- Cleaned controller readability while preserving the current app behavior.
- Kept beginner-facing comments where they help explain the code.

### Still intentionally temporary

- Local Facebook token storage is file-based for sandbox testing.
- Facebook OAuth flow is still experimental.
- Only Facebook text posting is implemented.
- Facebook image/photo posting is not implemented yet.
- Instagram, LinkedIn, TikTok, and YouTube integrations are not implemented yet.
- No user authentication or brokerage-level permissions yet.
- No production secret storage yet.
- Hangfire dashboard is not secured for production use.
- The app still contains an older generic caption/scheduling flow alongside the newer Facebook review/publish flow.

---

## Why This Project Exists

This project is a sandbox for learning how a real business application could support AI-assisted social posting.

The long-term product idea looks like this:

```text
Broker opens a listing
        ↓
AI generates platform-specific captions
        ↓
User reviews and edits the post
        ↓
User schedules or publishes
        ↓
App stores the post in SQL Server
        ↓
Hangfire/background service publishes when due
        ↓
Platform API returns success/failure
        ↓
App saves the result for auditing
```

Project structure
```text
ListingAutoPosterSandbox
├── ListingAutoPosterSandbox.sln
├── README.md
├── .gitignore
├── docs
│   └── retired-code
│       └── facebook-test-endpoint
│           ├── README.md
│           ├── FacebookTestController.cs.txt
│           ├── FacebookTestViewModel.cs.txt
│           └── Index.cshtml.txt
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
    │   ├── Listing.cs
    │   ├── ScheduledPost.cs
    │   ├── PostAttempt.cs
    │   ├── SocialAccount.cs
    │   ├── PostPlatform.cs
    │   └── PostStatus.cs
    ├── ViewModels
    │   ├── FacebookPostReviewViewModel.cs
    │   ├── GeneratedCaptionViewModel.cs
    │   └── CreateScheduledPostViewModel.cs
    ├── Services
    │   ├── Facebook
    │   │   ├── FacebookOAuthModels.cs
    │   │   ├── FacebookOAuthService.cs
    │   │   ├── FacebookOptions.cs
    │   │   ├── FacebookPagePoster.cs
    │   │   └── FacebookPostResult.cs
    │   ├── Development
    │   │   ├── FakePlatformPoster.cs
    │   │   └── FakeTokenStore.cs
    │   ├── ICaptionGenerator.cs
    │   ├── OpenAiCaptionGenerator.cs
    │   ├── IDuePostScanner.cs
    │   ├── DuePostScanner.cs
    │   ├── IScheduledPostPublisher.cs
    │   ├── ScheduledPostPublisher.cs
    │   ├── IPlatformPoster.cs
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
