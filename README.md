# Listing Auto Poster Sandbox

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge)
![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-512BD4?style=for-the-badge)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-blue?style=for-the-badge)
![Meta Graph API](https://img.shields.io/badge/Meta%20Graph%20API-Facebook%20Pages-0866FF?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Sandbox%20POC-orange?style=for-the-badge)

A learning sandbox for building an AI-assisted yacht listing social media auto-poster with ASP.NET Core MVC, Entity Framework Core, SQL Server LocalDB, Hangfire, OpenAI caption generation, scheduled post records, and Facebook Page publishing through the Meta Graph API.

This project is not a production-ready YATCO BOSS module. It is a focused sandbox used to learn, test, and document the implementation path one phase at a time.

---

## Current Purpose

The goal of this project is to model the core workflow behind a YATCO BOSS-style Social Media Auto-Poster:

1. Connect or review social accounts.
2. Select a yacht listing or enter custom yacht information.
3. Generate a yacht-broker-style AI caption.
4. Review and edit the generated caption.
5. Select one or more connected social accounts.
6. Choose a scheduled date and time.
7. Create one `ScheduledPost` row per selected account.
8. Let Hangfire scan for due posts.
9. Publish supported posts through the platform poster abstraction.
10. Store publish attempts, status updates, platform responses, and external post IDs.

The current real publishing implementation is Facebook Page text posting. The UI is being shaped toward multi-platform scheduling, but Instagram, LinkedIn, TikTok, and YouTube publishing are not implemented yet.

---

## Current Stage

This sandbox has moved from a basic fake social-posting pipeline into a yacht-focused scheduling proof of concept.

Current stage:

```text
Yacht-focused, Facebook-first sandbox with:
- AI caption generation
- custom yacht input
- multi-account checkbox scheduling
- scheduled post persistence
- Hangfire due-post scanning
- real Facebook Page text publishing
- polished navigation and UI pages
```

Still not production-ready:

```text
This is a learning and architecture proof of concept.
```

---

## Working Now

### Application Shell and Navigation

- Custom top navigation bar replacing the basic Bootstrap header.
- Persistent navigation to:
  - Dashboard
  - Listings
  - Social Accounts
  - Scheduled Posts
  - Hangfire
- Prominent **Connect Facebook Page** button available from the top navigation.
- Updated home/dashboard page that explains the main workflow areas without forcing a rigid step order.

### Social Accounts Page

- Dedicated Social Accounts page styled as a connection center.
- Prominent **Connect Facebook Page** action.
- Connection summary cards.
- Existing social accounts displayed as polished cards.
- Clearer explanation that Facebook Page connection should happen before scheduling posts.

### Yacht Listing Flow

- Yacht-focused listing cards instead of real estate/home cards.
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
- Custom yacht input card allows the user to enter custom information and send it to the AI caption generator.

### AI Caption Generation

- Uses OpenAI through an `ICaptionGenerator` abstraction.
- Prompt is tuned for yacht brokerage marketing copy.
- Caption generation is intended to avoid inventing yacht facts that were not provided.
- Generated copy is platform-neutral for the current sandbox flow.

### Multi-Account Scheduling UI

- Single-platform dropdown has been replaced with connected-account checkboxes.
- Selected social accounts are visually obvious:
  - unselected cards show a neutral state
  - selected cards show a darker yacht/ocean style with a visible checkmark
- User can select one or more connected social accounts.
- Submitting the form creates one `ScheduledPost` row per selected social account.
- This moves the sandbox closer to a real campaign-style workflow where one listing can produce multiple platform posts.

### Improved Schedule Picker

- Crude `datetime-local` field has been replaced with a clearer scheduling UI.
- User can choose:
  - date
  - time from a dropdown
  - quick preset options
- UI shows a readable schedule preview before submission.
- Existing backend form submission still uses the same `ScheduledLocal` value.

### Scheduled Post Pipeline

- `ScheduledPosts` table stores planned posts.
- `PostAttempts` table records publish attempts and platform responses.
- Hangfire recurring job scans for due scheduled posts.
- `ScheduledPostPublisher` sends due posts through the platform poster abstraction.
- Scheduled Posts page has a polished card-based UI with:
  - status badges
  - scheduled time
  - selected social account
  - platform
  - attempt count
  - external post ID
  - manual publish action
- Details page still shows deeper debugging information for post attempts and responses.

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

## Current Core Workflow

```text
User opens the app
        ↓
User connects or reviews social accounts
        ↓
User opens Listings
        ↓
User chooses a yacht listing or enters custom yacht information
        ↓
App generates a yacht-broker-style AI caption
        ↓
User reviews/edits the caption
        ↓
User selects one or more connected social accounts
        ↓
User chooses a scheduled date/time
        ↓
App creates one ScheduledPost row per selected account
        ↓
Hangfire due-post scan finds posts whose scheduled time has arrived
        ↓
ScheduledPostPublisher sends each due post to the matching platform poster
        ↓
FacebookPagePoster publishes supported Facebook text posts through the Meta Graph API
        ↓
PostAttempt records the platform result
        ↓
ScheduledPost stores status, attempt count, response JSON, and external post ID
```

---

## Current Architecture

```text
ASP.NET Core MVC
        ↓
Razor Views
        ↓
Controllers
        ↓
Application Services
        ↓
EF Core / SQL Server LocalDB
        ↓
Hangfire Background Jobs
        ↓
Platform Poster Abstraction
        ↓
FacebookPagePoster
        ↓
Meta Graph API
```

---

## Main Technologies

- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server LocalDB
- Hangfire
- Razor Views
- OpenAI API
- Meta Graph API
- Local user-secrets
- Local file-based token store for sandbox OAuth testing
- Bootstrap foundation with custom YATCO-inspired CSS

---

## Project Structure

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
    │   ├── css
    │   │   └── site.css
    │   ├── js
    │   └── lib
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json
```

---

## Important Pages

```text
/                  Dashboard and workflow overview
/Listings          Yacht listing cards and custom yacht input
/SocialAccounts    Connect/review social account records
/ScheduledPosts    Scheduled post queue and publish actions
/Hangfire          Local Hangfire dashboard
```

---

## Local Setup

### 1. Clone the repo

```powershell
git clone git@github.com:JegMc/Listing-Auto-Poster-Sandbox.git
cd Listing-Auto-Poster-Sandbox
```

### 2. Restore packages

```powershell
dotnet restore
```

### 3. Configure local user secrets

This project expects sensitive values to come from local user-secrets, not committed config files.

```powershell
cd .\ListingAutoPosterSandbox.Web
dotnet user-secrets init
```

Set your OpenAI API key:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY"
```

Optional model override:

```powershell
dotnet user-secrets set "OpenAI:Model" "gpt-5-mini"
```

For Facebook OAuth testing, set Meta app configuration through user-secrets as needed by your local code path:

```powershell
dotnet user-secrets set "Facebook:AppId" "YOUR_META_APP_ID"
dotnet user-secrets set "Facebook:AppSecret" "YOUR_META_APP_SECRET"
dotnet user-secrets set "Facebook:GraphApiVersion" "v20.0"
```

Do not commit real keys, tokens, app secrets, or local token files.

### 4. Apply database migrations

From the repo root:

```powershell
dotnet ef database update --project .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj
```

### 5. Build

```powershell
dotnet build
```

### 6. Run

```powershell
dotnet watch run --project .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj --launch-profile http
```

---

## Local Development Safety

The following should never be committed:

```text
App_Data/
facebook-tokens.local.json
.env
.env.*
real access tokens
real refresh tokens
Meta app secret
OpenAI API key
published output
local database files
```

Before pushing, run:

```powershell
git status
git ls-files | Select-String -Pattern "App_Data|facebook-tokens|\.env|secret|token|key"
dotnet build
```

If a real secret ever appears in Git history, revoke it immediately.

---

## Current Limitations

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

## Current Limitation: Multi-Platform UI vs Multi-Platform Publishing

The UI can schedule posts for multiple selected social accounts.

However, the backend currently has real publishing support only for Facebook text posts.

That means:

```text
Facebook scheduled rows:
- can publish through the Meta Graph API

Instagram scheduled rows:
- future integration target

LinkedIn scheduled rows:
- future integration target

TikTok scheduled rows:
- future integration target

YouTube scheduled rows:
- future integration target
```

This is intentional for the sandbox. The goal is to prove the workflow shape first, then add each platform client one at a time.

---

## YATCO BOSS Direction

This sandbox is moving toward the YATCO BOSS Social Media Auto-Poster concept:

- Connect brokerage social accounts.
- Generate AI captions from yacht listing data.
- Let the user review and edit content before publishing.
- Schedule posts across platforms.
- Store durable scheduled post rows.
- Use background jobs to publish due posts.
- Log platform responses and failures.
- Later collect engagement metrics.

A production version would eventually need:

- brokerage/user permissions
- production OAuth flows
- secure token storage, such as AWS Secrets Manager
- image staging, such as S3
- platform-specific API clients
- token refresh logic
- failure retry policies
- engagement metrics collection
- production logging and monitoring
- secured Hangfire dashboard
- full authentication and authorization

---

## Suggested Next Development Order

Recommended next development order:

1. Improve the Scheduled Post Details page so debugging/publishing information is easier to read.
2. Harden platform routing so unsupported platforms do not accidentally go through the Facebook poster.
3. Add cancel/edit behavior for pending scheduled posts.
4. Harden Facebook OAuth reconnect and token expiration display.
5. Add Facebook image/photo posting.
6. Add Instagram next, because it is also Meta-based.
7. Add LinkedIn after Instagram.
8. Add YouTube Shorts and TikTok later because video posting is more complex.
9. Add production-ready token storage and logging.

---

## Recent UI Improvements

Recent quality-of-life UI updates include:

- custom YATCO-inspired top navigation
- visible Social Accounts navigation
- persistent Connect Facebook Page call-to-action
- redesigned dashboard
- improved workflow explanation
- polished Listings page
- custom yacht input card
- redesigned generated-caption scheduling page
- clearer social account checkbox selection states
- better date/time scheduling controls
- redesigned Scheduled Posts page
- redesigned Social Accounts page
- updated CSS for yacht-focused cards, badges, buttons, forms, and responsive layout

---

## Status Summary

```text
Current stage:
Yacht-focused, Facebook-first sandbox with AI captions, multi-account scheduling UI,
scheduled post persistence, Hangfire due-post scanning, real Facebook text publishing,
and a more polished YATCO-inspired interface.

Not production-ready:
This remains a learning and architecture proof of concept.
```
