# Listing Auto Poster Sandbox

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge)
![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-512BD4?style=for-the-badge)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-blue?style=for-the-badge)
![Meta Graph API](https://img.shields.io/badge/Meta%20Graph%20API-Facebook%20Pages-0866FF?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Sandbox%20POC-orange?style=for-the-badge)

A learning sandbox for building an AI-assisted yacht listing social media auto-poster with ASP.NET Core MVC, Entity Framework Core, SQL Server LocalDB, Hangfire, OpenAI caption generation, scheduled post records, and Facebook Page text publishing through the Meta Graph API.

This project is not a production-ready YATCO BOSS module. It is a focused sandbox used to learn and prove the implementation path one phase at a time.

---

## Current Purpose

The goal of this project is to model the core workflow behind a YATCO BOSS-style Social Media Auto-Poster:

1. A user selects a yacht listing.
2. The app generates a yacht-broker-style AI caption.
3. The user reviews the generated caption.
4. The user selects one or more connected social accounts with checkboxes.
5. The user chooses a scheduled date/time.
6. The app creates one `ScheduledPost` row per selected social account.
7. Hangfire scans for due scheduled posts.
8. The platform poster publishes supported posts.
9. The app stores publish attempts, result details, and external platform IDs.

The current real publishing implementation is Facebook text posting. The UI is being shaped toward multi-platform scheduling, but Instagram, LinkedIn, TikTok, and YouTube publishing are not implemented yet.

---

## Working Now

### Yacht Listing Flow

- Yacht-focused sample listings instead of real estate/home listings.
- Listing data includes yacht-specific fields such as:
  - builder
  - brokerage/company
  - length
  - year built
  - cabins
  - guest capacity
  - max speed
  - location
  - price
  - description
- Listings page supports generating a generic scheduled social post.
- Obsolete `Draft Facebook Post` button has been removed from the main listing cards.
- Custom yacht card lets the user enter custom yacht information and send it to the AI caption generator.

### AI Caption Generation

- Uses OpenAI through an `ICaptionGenerator` abstraction.
- Prompt is tuned for yacht brokerage marketing copy.
- Caption generation avoids inventing yacht facts that were not provided.
- Generated copy is intended to be platform-neutral for the current sandbox flow.

### Multi-Account Scheduling UI

- The old single-platform dropdown has been changed to account checkboxes.
- User can select one or more connected social accounts.
- Submitting the form creates one `ScheduledPost` row per selected social account.
- This moves the sandbox closer to a real campaign-style workflow where one listing can produce multiple platform posts.

### Scheduled Post Pipeline

- `ScheduledPosts` table stores planned posts.
- `PostAttempts` table records publish attempts and results.
- Hangfire recurring job scans for due scheduled posts.
- `ScheduledPostPublisher` sends due posts through the platform poster abstraction.
- Details page shows post status, publish attempts, response JSON, and external platform post ID.

### Facebook Publishing

- Real Facebook Page text posting works through the Meta Graph API.
- `SocialAccount.PlatformAccountId` is used as the Facebook Page ID.
- `FacebookPagePoster` handles Facebook publishing.
- Meta's returned post ID is stored after a successful publish.

### OAuth / Token Work

- Facebook OAuth connection flow exists as a local sandbox proof.
- Local token storage is file-based and ignored by Git.
- Production token storage is not implemented yet.

---

## Still Intentionally Temporary

This repo is intentionally incomplete in the following areas:

- Facebook text posting is the only real platform publishing implementation.
- Instagram publishing is not implemented.
- LinkedIn publishing is not implemented.
- TikTok publishing is not implemented.
- YouTube Shorts publishing is not implemented.
- Facebook image/photo posting is not implemented yet.
- OAuth flow still needs hardening.
- Token expiration and reconnect handling are not complete.
- Local token storage is file-based for sandbox testing.
- Production secret storage is not implemented.
- User authentication and brokerage-level permissions are not implemented.
- Hangfire dashboard is for local development only and is not production-secured.
- Engagement metrics collection is not implemented.
- Retired fake/test code is kept under `docs/retired-code` for documentation history.

---

## Current Core Workflow

```text
User opens Listings
        ↓
User reviews yacht listing cards or enters a custom yacht
        ↓
User clicks Generate Generic Scheduled Post
        ↓
App generates a yacht-broker-style AI caption
        ↓
User reviews/edits the caption
        ↓
User selects one or more connected social accounts with checkboxes
        ↓
User chooses a scheduled date/time
        ↓
App creates one ScheduledPost row per selected social account
        ↓
Hangfire due-post scan finds posts whose scheduled time has arrived
        ↓
ScheduledPostPublisher sends each due post to the matching platform poster
        ↓
FacebookPagePoster publishes Facebook text posts through the Meta Graph API
        ↓
PostAttempt records the platform result
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
    │   ├── CustomListingInputViewModel.cs
    │   ├── FacebookPostReviewViewModel.cs
    │   └── GeneratedCaptionViewModel.cs
    ├── Services
    │   ├── Facebook
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
