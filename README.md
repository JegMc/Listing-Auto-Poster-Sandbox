# Listing Auto Poster Sandbox

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge)
![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-512BD4?style=for-the-badge)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-00A1E0?style=for-the-badge)
![Meta Graph API](https://img.shields.io/badge/Meta%20Graph%20API-Facebook%20%2B%20Instagram-0866FF?style=for-the-badge)
![OpenAI](https://img.shields.io/badge/OpenAI-Caption%20Generation-111827?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Sandbox%20POC-orange?style=for-the-badge)

A focused ASP.NET Core MVC sandbox for AI-assisted yacht social media posting.

The app lets a user create yacht listing content, generate AI captions, schedule posts, publish due posts through Hangfire, and test real Meta Graph API publishing for Facebook Pages and Instagram public-image posts.

> This is not production YATCO BOSS code. It is a learning and architecture proof of concept.

---

## Current Status

```text
Working:
- Yacht listing and custom yacht input
- OpenAI caption generation
- Optional image-aware caption generation
- Multi-account scheduling
- Scheduled post edit/cancel/remove workflow
- Hangfire due-post scanning
- Facebook Page text publishing
- Facebook Page image publishing
- Instagram account diagnostic/discovery
- Instagram SocialAccount metadata saving
- Instagram single-image public URL publishing

Not built yet:
- Instagram local image staging
- Instagram carousel/Reels publishing
- LinkedIn publishing
- TikTok publishing
- YouTube Shorts publishing
- Production token storage
- Production auth/permissions
- Engagement metrics
```

Core Workflow 
```Text
User opens Listings
        ↓
User selects a yacht listing or enters custom yacht info
        ↓
App generates a yacht-broker-style AI caption
        ↓
User reviews/edits caption
        ↓
User selects one or more connected social accounts
        ↓
User optionally attaches an image
        ↓
App creates one ScheduledPost per selected account
        ↓
User may edit, cancel, publish now, or remove failed/cancelled posts
        ↓
Hangfire scans for due posts
        ↓
ScheduledPostPublisher routes to the correct platform poster
        ↓
FacebookPagePoster or InstagramPlatformPoster publishes through Meta Graph API
        ↓
PostAttempt records response JSON, errors, external IDs, and status
```

Current Functionality 
```Text
AI Caption Generation
Uses an ICaptionGenerator abstraction.
Current implementation uses OpenAI.
Prompts are tuned for yacht brokerage marketing copy.
Caption generation avoids inventing yacht facts when possible.
Public image URLs and uploaded local images can be used as optional visual context.
If image context fails, caption generation falls back to text-only.
Scheduling
Users can schedule one post to one or more connected social accounts.
Each selected account creates its own ScheduledPost row.
Scheduled posts support:
publish now
edit
cancel
remove failed/cancelled sandbox rows
view details and publish attempts
Facebook Publishing
Real Facebook Page publishing works through Meta Graph API.
Text-only posts publish to the Page feed endpoint.
Image posts publish to the Page photos endpoint.
Public image URLs can be published by URL.
Local uploaded images can be sent to Facebook by server-side file upload.
Instagram Publishing
Instagram setup diagnostics work through the connected Facebook Page.
The app can discover the connected Instagram Business account.
The discovered Instagram Graph account ID can be saved into SocialAccount.
Real Instagram publishing works for single-image posts with a public HTTPS image URL.
Instagram publishing uses the media-container flow:
create media container
poll/check container status
publish media container

Important Instagram limitation:

Instagram posts require a public HTTPS image URL.

Local upload paths and localhost URLs are not usable by Instagram directly.
A future staging step, such as S3 or another public asset host, is needed for local uploads.
```

Project Structure
```Text
Listing-Auto-Poster-Sandbox
├── ListingAutoPosterSandbox.sln
├── README.md
├── .gitignore
├── docs
│   └── retired-code
└── ListingAutoPosterSandbox.Web
    ├── Controllers
    │   ├── FacebookOAuthController.cs
    │   ├── HomeController.cs
    │   ├── InstagramDiagnosticsController.cs
    │   ├── ListingsController.cs
    │   ├── ScheduledPostsController.cs
    │   └── SocialAccountsController.cs
    │
    ├── Data
    │   └── AppDbContext.cs
    │
    ├── Migrations
    │
    ├── Models
    │   ├── Listing.cs
    │   ├── PostAttempt.cs
    │   ├── PostPlatform.cs
    │   ├── PostResult.cs
    │   ├── PostStatus.cs
    │   ├── ScheduledPost.cs
    │   └── SocialAccount.cs
    │
    ├── Services
    │   ├── Facebook
    │   │   ├── FacebookOAuthService.cs
    │   │   ├── FacebookOptions.cs
    │   │   └── FacebookPagePoster.cs
    │   │
    │   ├── Instagram
    │   │   ├── InstagramConnectionDiagnosticResult.cs
    │   │   ├── InstagramConnectionDiagnosticService.cs
    │   │   ├── InstagramDiagnosticOptions.cs
    │   │   ├── InstagramPlatformPoster.cs
    │   │   ├── InstagramPostReadinessResult.cs
    │   │   └── InstagramPostReadinessValidator.cs
    │   │
    │   ├── DuePostScanner.cs
    │   ├── IDuePostScanner.cs
    │   ├── IPlatformPoster.cs
    │   ├── IScheduledPostPublisher.cs
    │   ├── ITokenStore.cs
    │   ├── LocalFacebookTokenStore.cs
    │   ├── OpenAiCaptionGenerator.cs
    │   └── ScheduledPostPublisher.cs
    │
    ├── ViewModels
    │   ├── CreateScheduledPostViewModel.cs
    │   ├── CustomListingInputViewModel.cs
    │   ├── EditScheduledPostViewModel.cs
    │   └── GeneratedCaptionViewModel.cs
    │
    ├── Views
    │   ├── FacebookOAuth
    │   ├── Home
    │   ├── InstagramDiagnostics
    │   ├── Listings
    │   ├── ScheduledPosts
    │   ├── Shared
    │   └── SocialAccounts
    │
    ├── wwwroot
    │   ├── css
    │   ├── js
    │   ├── lib
    │   └── uploads
    │       └── listings
    │
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json
```

Setup
```Text
1. Clone the repo
git clone git@github.com:JegMc/Listing-Auto-Poster-Sandbox.git
cd Listing-Auto-Poster-Sandbox
2. Restore packages
dotnet restore
3. Install EF tooling if needed
dotnet tool install --global dotnet-ef

If already installed:

dotnet tool update --global dotnet-ef
4. Configure user-secrets

Go into the web project:

cd .\ListingAutoPosterSandbox.Web
dotnet user-secrets init

Set OpenAI config:

dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY"
dotnet user-secrets set "OpenAI:Model" "gpt-5-mini"

Set Meta/Facebook config:

dotnet user-secrets set "Facebook:AppId" "YOUR_META_APP_ID"
dotnet user-secrets set "Facebook:AppSecret" "YOUR_META_APP_SECRET"
dotnet user-secrets set "Facebook:GraphApiVersion" "v20.0"

Set Instagram diagnostic config:

dotnet user-secrets set "InstagramDiagnostic:FacebookPageId" "YOUR_FACEBOOK_PAGE_ID"
dotnet user-secrets set "InstagramDiagnostic:ExpectedInstagramUsername" "YOUR_INSTAGRAM_USERNAME"
dotnet user-secrets set "InstagramDiagnostic:GraphApiVersion" "v20.0"

Return to the repo root:

cd ..
Meta / Instagram Setup

To test Facebook and Instagram publishing locally, you need:

1. A Meta developer app.
2. A Facebook Page you control.
3. An Instagram Business or Creator account.
4. The Instagram account connected to the Facebook Page.
5. A test Facebook user that has access to the Page and app.
6. Meta permissions requested during the OAuth/connect flow.

Required permissions for the current sandbox flow:

pages_show_list
pages_read_engagement
pages_manage_posts
instagram_basic
instagram_content_publish

Recommended setup order:

1. Create or open your Meta developer app.
2. Add/configure Facebook Login if needed by the local OAuth flow.
3. Connect your Instagram Business/Creator account to your Facebook Page.
4. Set the Facebook Page ID in user-secrets.
5. Run the app.
6. Go to Social Accounts.
7. Connect the Facebook Page.
8. Go to Instagram Diagnostics.
9. Confirm the connected Instagram account is discovered.
10. Save/update the Instagram SocialAccount row.
11. Test Instagram publishing with a public HTTPS image URL.

Do not commit access tokens, app secrets, screenshots containing tokens, or local token files.

Database Setup

From the repo root:

dotnet ef database update `
  --project .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj `
  --startup-project .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj
Build and Run

Build:

dotnet build .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj

Run:

dotnet watch run --project .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj --launch-profile http

Then open the local URL shown in the terminal.
```
