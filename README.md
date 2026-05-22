# Listing Auto Poster Sandbox

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge)
![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-512BD4?style=for-the-badge)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-blue?style=for-the-badge)
![Meta Graph API](https://img.shields.io/badge/Meta%20Graph%20API-Facebook%20Pages-0866FF?style=for-the-badge)
![OpenAI](https://img.shields.io/badge/OpenAI-Caption%20Generation-111827?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Sandbox%20POC-orange?style=for-the-badge)

A learning sandbox for building an AI-assisted yacht listing social media auto-poster with ASP.NET Core MVC, Entity Framework Core, SQL Server LocalDB, Hangfire, OpenAI caption generation, scheduled post records, optional hero-image handling, and Facebook Page publishing through the Meta Graph API.

This project is not a production-ready YATCO BOSS module. It is a focused sandbox used to learn, test, and document the implementation path one phase at a time.

---

## Current Purpose

The goal of this project is to model the core workflow behind a YATCO BOSS-style Social Media Auto-Poster:

1. Connect or review social accounts.
2. Select a yacht listing or enter custom yacht information.
3. Generate a yacht-broker-style AI caption.
4. Optionally let the hero image influence caption generation.
5. Review and edit the generated caption.
6. Select one or more connected social accounts.
7. Choose whether to attach the hero image to the scheduled post.
8. Choose a scheduled date and time.
9. Create one `ScheduledPost` row per selected account.
10. Let Hangfire scan for due posts.
11. Publish supported posts through the platform poster abstraction.
12. Store publish attempts, status updates, platform responses, and external post IDs.
13. Edit or cancel pending scheduled posts before they publish.

The current real publishing implementation is Facebook Page publishing. The sandbox supports text-only Facebook posts and optional Facebook photo posts when the user explicitly attaches the hero image. Instagram, LinkedIn, TikTok, and YouTube publishing are future integration targets.

---

## Current Stage

This sandbox has moved from a basic fake social-posting pipeline into a yacht-focused scheduling proof of concept.

Current stage:

```text
Yacht-focused, Facebook-first sandbox with:
- AI caption generation
- custom yacht input
- optional hero-image input
- optional image-aware caption generation
- multi-account checkbox scheduling
- scheduled post persistence
- edit/cancel actions for scheduled posts
- Hangfire due-post scanning
- real Facebook Page text publishing
- optional Facebook Page photo publishing
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
  - image URL
- Listings page supports generating a generic scheduled social post.
- Obsolete `Draft Facebook Post` button has been removed from the main listing cards.
- Custom yacht input card allows the user to enter custom information and send it to the AI caption generator.

### Custom Yacht Input

The custom input card supports:

- freeform yacht/marketing notes
- yacht/listing title
- builder
- company/brokerage
- location
- length
- year built
- cabins
- guest capacity
- max speed
- price
- public image URL
- local uploaded hero image

Local uploaded images are stored under the sandbox app’s `wwwroot/uploads/listings` area and should not be committed to Git.

### AI Caption Generation

- Uses OpenAI through an `ICaptionGenerator` abstraction.
- Prompt is tuned for yacht brokerage marketing copy.
- Caption generation is intended to avoid inventing yacht facts that were not provided.
- Generated copy is platform-neutral for the current sandbox flow.
- The generator can use a hero image as visual context when available.
- Image context is optional and fail-safe:
  - valid public image URLs can be used as image context
  - uploaded local images can be converted and used as image context
  - invalid, blocked, oversized, or unsupported images fall back to text-only caption generation

### Image Compatibility

Supported input styles:

```text
Public image URL:
- Can be used for browser preview.
- Can be used as OpenAI visual context if the host returns valid image bytes.
- Can be used for Facebook photo publishing if the URL is publicly reachable by Meta.

Local upload through the app:
- Can be previewed by the sandbox app.
- Can be converted to image input for OpenAI caption generation.
- Can be uploaded to Facebook directly by the server as a multipart file.

Raw local file path:
- Not supported.
- Example that will not work: C:\Users\...\boat.jpg
- A browser/server cannot safely send a user’s raw local filesystem path to OpenAI or Facebook.

Localhost URL:
- Useful for local browser preview.
- Not reliable for Facebook URL-based publishing because Meta cannot fetch localhost.
- The sandbox handles app-uploaded local files by uploading file bytes directly to Facebook instead.
```

Supported image file types:

```text
.jpg
.jpeg
.png
.webp
.gif for OpenAI image context where accepted
```

### Multi-Account Scheduling UI

- Single-platform dropdown has been replaced with connected-account checkboxes.
- Selected social accounts are visually obvious:
  - unselected cards show a neutral state
  - selected cards show a darker yacht/ocean style with a visible checkmark
- User can select one or more connected social accounts.
- Submitting the form creates one `ScheduledPost` row per selected social account.
- This moves the sandbox closer to a campaign-style workflow where one listing can produce multiple platform posts.

### Optional Hero Image Attachment

The generated-caption scheduling page includes an explicit image choice:

```text
Attach hero image to scheduled post
```

This distinction matters:

```text
Listing.ImageUrl:
The listing’s default/hero image.

ScheduledPost.ImageUrl:
The image actually attached to this specific scheduled post.
```

If the user leaves the image option unchecked, the scheduled post is text-only.

If the user checks the image option, the scheduled post carries the image forward for platform publishing.

### Improved Schedule Picker

- Crude `datetime-local` field has been replaced with a clearer scheduling UI.
- User can choose:
  - date
  - time from a dropdown
  - quick preset options
- UI shows a readable schedule preview before submission.
- Existing backend form submission still uses the same `ScheduledLocal` value.

### Edit and Cancel Scheduled Posts

Scheduled posts can now be managed before publishing:

- pending scheduled posts can be edited
- pending scheduled posts can be cancelled
- edited posts can update caption and scheduled time
- cancelled posts are excluded from normal due-post publishing
- posted/processing/cancelled posts are protected from inappropriate edits

This makes the sandbox closer to a real scheduled-post workflow, where users can change their mind before content goes live.

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
  - details action
  - edit action when allowed
  - cancel action when allowed
  - manual publish action when allowed
- Details page shows deeper debugging information for post attempts and responses.

### Facebook Publishing

- Real Facebook Page publishing works through the Meta Graph API.
- `SocialAccount.PlatformAccountId` is used as the Facebook Page ID.
- `FacebookPagePoster` handles Facebook publishing.
- Facebook text-only posts use the Page feed endpoint.
- Facebook image posts use the Page photos endpoint when a scheduled post has an attached image.
- Meta's returned post ID is stored after a successful publish.
- Facebook API failures are captured in post attempts and shown in the UI.

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
User optionally provides a public image URL or uploads a hero image
        ↓
App generates a yacht-broker-style AI caption
        ↓
Image context may influence the caption when available and valid
        ↓
User reviews/edits the caption
        ↓
User selects one or more connected social accounts
        ↓
User chooses whether to attach the hero image to the scheduled post
        ↓
User chooses a scheduled date/time
        ↓
App creates one ScheduledPost row per selected account
        ↓
User may edit or cancel scheduled posts before publishing
        ↓
Hangfire due-post scan finds posts whose scheduled time has arrived
        ↓
ScheduledPostPublisher sends each due post to the matching platform poster
        ↓
FacebookPagePoster publishes supported Facebook text/photo posts through the Meta Graph API
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

Image-aware caption flow:

```text
Listing / Custom Input
        ↓
Image URL or uploaded image
        ↓
OpenAiCaptionGenerator
        ↓
Image validated / converted when possible
        ↓
OpenAI caption generation
        ↓
Fallback to text-only if image context fails
```

Facebook image-publishing flow:

```text
ScheduledPost.ImageUrl is empty
        ↓
Publish text-only Facebook feed post

ScheduledPost.ImageUrl has public URL
        ↓
Publish Facebook photo post by URL

ScheduledPost.ImageUrl has local upload path
        ↓
Server uploads local image bytes to Facebook
        ↓
Publish Facebook photo post
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
    │   ├── EditScheduledPostViewModel.cs
    │   ├── FacebookPostReviewViewModel.cs
    │   └── GeneratedCaptionViewModel.cs
    ├── Services
    │   ├── Facebook
    │   │   ├── FacebookOAuthService.cs
    │   │   ├── FacebookOptions.cs
    │   │   └── FacebookPagePoster.cs
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
    │   ├── lib
    │   └── uploads
    │       └── listings
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json
```

Note: `wwwroot/uploads` is for local sandbox file uploads and should be ignored by Git.

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
dotnet ef database update --project .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj --startup-project .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj
```

### 5. Build

```powershell
dotnet build .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj
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
wwwroot/uploads/
uploaded test images
```

Before pushing, run:

```powershell
git status
git status --ignored
git ls-files | Select-String -Pattern "uploads|App_Data|facebook-tokens|\.env|secret|token|key"
dotnet build .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj
```

Seeing source-code names like `ITokenStore`, `access_token`, or config key names is acceptable.

You should not see:

```text
real API keys
real OAuth tokens
real app secrets
uploaded image files
local token files
.env files
```

If a real secret ever appears in Git history, revoke it immediately.

---

## Testing Checklist

### Text-only Facebook post

```text
1. Go to Listings.
2. Choose a yacht listing or create a custom yacht post.
3. Generate Generic Scheduled Post.
4. Leave “Attach hero image to scheduled post” unchecked.
5. Select Facebook.
6. Schedule or publish now.
```

Expected:

```text
ScheduledPost.ImageUrl is empty/null.
Facebook publishes a text-only feed post.
```

### Facebook post with public image URL

```text
1. Go to Listings.
2. Use the custom yacht form.
3. Paste a public JPG/PNG/WebP image URL.
4. Generate Generic Scheduled Post.
5. Check “Attach hero image to scheduled post.”
6. Select Facebook.
7. Schedule or publish now.
```

Expected:

```text
ScheduledPost.ImageUrl has a public URL.
Facebook publishes a photo post using the image URL.
```

### Facebook post with local uploaded image

```text
1. Go to Listings.
2. Use the custom yacht form.
3. Upload a local JPG/PNG/WebP image.
4. Generate Generic Scheduled Post.
5. Check “Attach hero image to scheduled post.”
6. Select Facebook.
7. Schedule or publish now.
```

Expected:

```text
ScheduledPost.ImageUrl stores a local app upload path.
Facebook publishes a photo post by uploading file bytes from the server.
```

### Edit scheduled post

```text
1. Create a scheduled post for the future.
2. Go to Scheduled Posts.
3. Click Edit.
4. Change the caption and/or scheduled time.
5. Save.
```

Expected:

```text
The scheduled post updates without creating a duplicate post.
```

### Cancel scheduled post

```text
1. Create a scheduled post for the future.
2. Go to Scheduled Posts.
3. Click Cancel.
```

Expected:

```text
The post status changes to Cancelled.
The cancelled post does not publish when the due-post scanner runs.
```

---

## Current Limitations

This repo is intentionally incomplete in the following areas:

- Facebook is the only real platform publishing implementation.
- Instagram publishing is not implemented.
- LinkedIn publishing is not implemented.
- TikTok publishing is not implemented.
- YouTube Shorts publishing is not implemented.
- OAuth flow still needs hardening.
- Token expiration and reconnect handling are not complete.
- Local token storage is file-based for sandbox testing.
- Production secret storage is not implemented.
- Uploaded images are stored locally, not in S3.
- User authentication and brokerage-level permissions are not implemented.
- Hangfire dashboard is for local development only and is not production-secured.
- Engagement metrics collection is not implemented.
- Retired fake/test code is kept under `docs/retired-code` for documentation history.

---

## Current Limitation: Multi-Platform UI vs Multi-Platform Publishing

The UI can schedule posts for multiple selected social accounts.

However, the backend currently has real publishing support only for Facebook.

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

1. Harden platform routing so unsupported platforms are clearly marked as demo/future integrations.
2. Improve the Scheduled Post Details page so debugging/publishing information is easier to read.
3. Harden Facebook OAuth reconnect and token expiration display.
4. Improve image validation and user-facing image error messages.
5. Add clearer UI for attached vs. text-only posts.
6. Add Instagram next, because it is also Meta-based.
7. Add LinkedIn after Instagram.
8. Add YouTube Shorts and TikTok later because video posting is more complex.
9. Add production-ready token storage and logging.

---

## Recent Functional Improvements

Recent functionality updates include:

- scheduled post edit action
- scheduled post cancel action
- explicit Cancelled post status
- optional hero image attachment per scheduled post
- image-aware OpenAI caption generation
- text-only fallback when image input fails
- local image upload support
- public image URL support
- Facebook text-only publishing
- Facebook photo publishing by public URL
- Facebook photo publishing by local uploaded file bytes

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
- optional hero-image attach control
- better date/time scheduling controls
- redesigned Scheduled Posts page
- redesigned Social Accounts page
- updated CSS for yacht-focused cards, badges, buttons, forms, and responsive layout

---

## Status Summary

```text
Current stage:
Yacht-focused, Facebook-first sandbox with AI captions, custom yacht input,
optional image-aware caption generation, multi-account scheduling UI,
scheduled post persistence, edit/cancel support, Hangfire due-post scanning,
real Facebook text/photo publishing, and a polished YATCO-inspired interface.

Not production-ready:
This remains a learning and architecture proof of concept.
```
