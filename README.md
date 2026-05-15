# Listing Auto Poster Sandbox

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4)
![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-blue)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-orange)
![Status](https://img.shields.io/badge/Status-Local%20Sandbox-lightgrey)

A portfolio sandbox project that demonstrates how an AI-assisted social media scheduling pipeline can be built with ASP.NET Core, EF Core, SQL Server, Hangfire, OpenAI, and clean service abstractions.

This project models the shape of a real social auto-poster without using real social media APIs or real OAuth credentials.

---

## Project Snapshot

| Area | Implementation |
|---|---|
| Web framework | ASP.NET Core MVC |
| Language | C# |
| Runtime | .NET 9 |
| Database | SQL Server LocalDB |
| ORM | Entity Framework Core |
| Background jobs | Hangfire |
| AI caption generation | OpenAI .NET SDK |
| Social posting | Fake platform publisher |
| Token handling | Fake token-store abstraction |
| Status tracking | Scheduled post status + publish attempts |

---

## Core Workflow

```text
Listing
  -> AI caption generation
  -> user edits caption
  -> user selects social account
  -> user schedules post
  -> ScheduledPost is saved to SQL Server
  -> Hangfire scans for due posts
  -> fake token store retrieves token by SecretName
  -> fake platform publisher runs
  -> PostAttempt is logged
  -> ScheduledPost becomes Posted or Failed
```

---

## What This Project Demonstrates

This repo is intended to show backend development concepts that are common in business applications:

| Concept | Where it appears |
|---|---|
| MVC structure | Controllers, Views, Models |
| Dependency injection | Services registered in `Program.cs` |
| Database modeling | `Listing`, `ScheduledPost`, `SocialAccount`, `PostAttempt` |
| EF Core migrations | `Migrations` folder |
| Background processing | Hangfire recurring scanner job |
| AI integration | `OpenAiCaptionGenerator` |
| Service abstraction | `ICaptionGenerator`, `ITokenStore`, `IPlatformPoster` |
| Safe token pattern | SQL stores `SecretName`, not raw tokens |
| Audit logging | `PostAttempt` records each publish attempt |
| UTC scheduling | Database stores UTC, UI displays Eastern time |

---

## Features

### Listings

- Displays sample real estate listings.
- Lets the user choose a listing for caption generation.

### AI Caption Generation

- Sends listing details to OpenAI.
- Generates a social media caption.
- Allows the user to review and edit the caption before scheduling.

### Scheduling

- User selects a connected demo social account.
- User chooses a local schedule time.
- App stores the scheduled post in SQL Server.

### Background Publishing

- Hangfire runs a recurring scanner every minute.
- Due posts are queued for publishing.
- A fake publisher simulates a successful platform post.
- Post status updates automatically.

### Attempt Logging

- Every publish attempt creates a `PostAttempt` record.
- The details page shows:
  - attempt start time
  - completion time
  - success state
  - fake response JSON
  - token fingerprint

---

## Main Pages

| Page | Purpose |
|---|---|
| `/Listings` | View sample listings and generate captions |
| `/ScheduledPosts` | View scheduled and published posts |
| `/ScheduledPosts/Details/{id}` | Inspect post details and publish attempts |
| `/hangfire` | View Hangfire dashboard |

---

## Project Structure

```text
ListingAutoPosterSandbox
├── ListingAutoPosterSandbox.sln
├── README.md
├── .gitignore
└── ListingAutoPosterSandbox.Web
    ├── Controllers
    │   ├── HomeController.cs
    │   ├── ListingsController.cs
    │   └── ScheduledPostsController.cs
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
    ├── Services
    │   ├── ICaptionGenerator.cs
    │   ├── OpenAiCaptionGenerator.cs
    │   ├── IDuePostScanner.cs
    │   ├── DuePostScanner.cs
    │   ├── IScheduledPostPublisher.cs
    │   ├── ScheduledPostPublisher.cs
    │   ├── IPlatformPoster.cs
    │   ├── FakePlatformPoster.cs
    │   ├── ITokenStore.cs
    │   └── FakeTokenStore.cs
    ├── Views
    ├── wwwroot
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json
```

---

## Database Tables

| Table | Purpose |
|---|---|
| `Listings` | Stores sample real estate listings |
| `ScheduledPosts` | Stores captions, schedule times, platform, status, and result data |
| `SocialAccounts` | Stores fake connected account metadata and secret references |
| `PostAttempts` | Stores each publish attempt and response |
| Hangfire tables | Store Hangfire jobs, state, servers, and recurring job metadata |

Important security pattern:

```text
SocialAccounts.SecretName stores a reference like:
dev/social/facebook/demo-page

It does not store a real access token.
```

---

## Local Setup

### 1. Clone the repository

```powershell
git clone git@github.com:JegMc/Listing-Auto-Poster-Sandbox.git
cd Listing-Auto-Poster-Sandbox
```

### 2. Restore packages

```powershell
dotnet restore
```

### 3. Configure OpenAI user-secrets

Move into the web project:

```powershell
cd .\ListingAutoPosterSandbox.Web
```

Initialize user-secrets:

```powershell
dotnet user-secrets init
```

Set your OpenAI API key:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY"
```

Set the OpenAI model:

```powershell
dotnet user-secrets set "OpenAI:Model" "gpt-5-mini"
```

Return to the solution root:

```powershell
cd ..
```

### 4. Install EF Core CLI tool

```powershell
dotnet tool install --global dotnet-ef --version 9.0.0
```

If already installed:

```powershell
dotnet tool update --global dotnet-ef --version 9.0.0
```

### 5. Create or update the local database

```powershell
dotnet ef database update --project .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj
```

### 6. Run the app

```powershell
dotnet watch run --project .\ListingAutoPosterSandbox.Web\ListingAutoPosterSandbox.Web.csproj --launch-profile http
```

Open:

```text
http://localhost:5080
```

---

## Demo Walkthrough

1. Open the app.
2. Go to `Listings`.
3. Click `Generate Caption`.
4. Wait for OpenAI to generate a caption.
5. Edit the caption if needed.
6. Select a demo social account.
7. Schedule the post for 1-2 minutes in the future.
8. Go to `Scheduled Posts`.
9. Wait for Hangfire to process the post.
10. Refresh the page.
11. Confirm the status changes to `Posted`.
12. Click `Details`.
13. Review the publish attempt and fake response JSON.

---

## Hangfire Behavior

This app uses a scanner pattern instead of creating one delayed Hangfire job per post.

```text
Recurring Hangfire job
  -> runs every minute
  -> checks ScheduledPosts table
  -> finds posts where ScheduledUtc <= now
  -> queues publish job
  -> publisher updates status and logs attempt
```

This means scheduled posts appear first in the app’s `ScheduledPosts` table. Once they become due, Hangfire queues and runs the publishing job.

---

## Security Notes

This project is safe for local development and portfolio review, but it is not production-ready.

Current safety choices:

- OpenAI API key is stored with .NET user-secrets.
- No real social access tokens are committed.
- No real OAuth credentials are used.
- `FakeTokenStore` returns fake local tokens.
- `FakePlatformPoster` logs only a token fingerprint, not a raw token.
- SQL stores a `SecretName` reference, not an actual token.

Before any public deployment, the Hangfire dashboard should be protected with authentication and authorization.

---

## Current Limitations

This project does not currently include:

- real Facebook posting
- real Instagram posting
- real LinkedIn posting
- real OAuth authorization flow
- AWS Secrets Manager integration
- user authentication
- protected Hangfire dashboard
- automated tests
- deployed hosting configuration

---

## Possible Future Improvements

- Add authentication.
- Protect the Hangfire dashboard.
- Add post editing after scheduling.
- Add post cancellation.
- Add retry policy controls.
- Add unit tests for scheduling and publishing services.
- Add integration tests for the main workflow.
- Replace `FakeTokenStore` with AWS Secrets Manager.
- Replace `FakePlatformPoster` with real platform clients.
- Add screenshots or a short demo GIF.

---

## Portfolio Summary

This project shows a complete local workflow for an AI-assisted scheduled posting system:

```text
AI generation
+ human review
+ SQL persistence
+ background jobs
+ token abstraction
+ fake external API integration
+ publish logging
```

It is designed as a learning project and architecture sandbox, not a production social media automation tool.
