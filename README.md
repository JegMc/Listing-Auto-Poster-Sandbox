# Listing Auto Poster Sandbox

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge)
![EF Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-2C7A7B?style=for-the-badge)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-orange?style=for-the-badge)
![Meta Graph API](https://img.shields.io/badge/Meta%20Graph%20API-Facebook%20Posting-1877F2?style=for-the-badge&logo=facebook&logoColor=white)
![Status](https://img.shields.io/badge/Status-Active%20Sandbox-brightgreen?style=for-the-badge)

A learning sandbox for building an AI-assisted social media auto-poster with ASP.NET Core, Entity Framework Core, SQL Server LocalDB, Hangfire, OpenAI caption generation, and real Facebook Page posting through the Meta Graph API.

This repo started as a fake social-posting pipeline, but it now has a working Facebook proof of concept:

- pick a listing
- generate an AI-written Facebook caption
- review and edit the caption before publishing
- create a durable `ScheduledPost` database row
- publish through the same scheduled-post pipeline used by the app
- send the final text to a real Facebook Page
- save Meta's returned post ID and the publish attempt details

This is still a sandbox, not a production-ready social media tool. The point is to prove the architecture and learn the implementation path one phase at a time.

---

## Current Project Status

### Working now

- ASP.NET Core MVC dashboard
- sample listing cards
- OpenAI caption generation
- review/edit page before Facebook publishing
- `ScheduledPosts` database pipeline
- `PostAttempts` logging
- real Facebook Page text posting through Meta Graph API
- `SocialAccount.PlatformAccountId` used as the Facebook Page ID
- temporary local Page token stored with `.NET user-secrets`
- Hangfire dashboard and recurring background scan flow
- details page showing post status, attempts, response JSON, and external post ID

### Still intentionally temporary

- Facebook token is manually stored in local user-secrets
- OAuth is not finished yet
- only Facebook text posting is implemented
- no image upload/posting yet
- no user authentication
- no production secret storage
- Hangfire dashboard is not production-secured
- fake service files may still exist from earlier phases, but the active Facebook test path now uses the real `FacebookPagePoster`

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
