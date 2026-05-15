# Listing Auto Poster Sandbox

A portfolio sandbox project for learning how an AI-assisted social media scheduling pipeline can be built with ASP.NET Core, EF Core, SQL Server, Hangfire, and service abstractions.

The app simulates a real estate listing auto-poster flow:

```text
Listing
-> AI caption generation
-> user edits and schedules the post
-> ScheduledPost is saved to SQL Server
-> Hangfire scans for due posts
-> fake platform publisher runs
-> fake token store is used
-> publish attempt is logged
-> post status updates to Posted or Failed

Current Features
ASP.NET Core MVC web app
Real estate listing display
OpenAI-powered caption generation
Editable generated captions
Social account selection
Scheduled post creation
SQL Server persistence through EF Core
EF Core migrations
Hangfire recurring background job
Hangfire dashboard
Fake social platform publishing
Fake token-store abstraction
Publish attempt logging
Scheduled post details page
Eastern time display with UTC storage
Tech Stack
C#
ASP.NET Core MVC
.NET 9
Entity Framework Core
SQL Server LocalDB
Hangfire
OpenAI .NET SDK
Bootstrap
Project Structure
ListingAutoPosterSandbox
├── ListingAutoPosterSandbox.sln
├── .gitignore
└── ListingAutoPosterSandbox.Web
    ├── Controllers
    ├── Data
    ├── Migrations
    ├── Models
    ├── Properties
    ├── Services
    ├── Views
    ├── wwwroot
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json
Main Application Flow
1. Listings

The user starts on the Listings page and selects a sample real estate listing.

2. AI Caption Generation

The app sends listing details to OpenAI through OpenAiCaptionGenerator.

The generated caption is displayed to the user for review and editing.

3. Scheduling

The user chooses:

social account
edited caption
scheduled local time

The app saves a ScheduledPost row in SQL Server.

4. Background Processing

Hangfire runs a recurring scanner job once per minute.

The scanner finds scheduled posts where:

Status == Scheduled
ScheduledUtc <= DateTime.UtcNow

Each due post is queued for publishing.

5. Fake Publishing

The publishing service loads the scheduled post, loads the selected social account, retrieves a fake access token from FakeTokenStore, and passes the post plus token to FakePlatformPoster.

The fake platform returns:

success status
fake external post ID
fake response JSON
token fingerprint

The app saves the result to the database and creates a PostAttempt record.

Database Tables

The app currently uses these main tables:

Listings
ScheduledPosts
PostAttempts
SocialAccounts
Hangfire tables
Listings

Stores sample real estate listing data.

ScheduledPosts

Stores scheduled social media posts, including caption, platform, scheduled time, status, attempt count, external post ID, and error state.

PostAttempts

Stores each publish attempt, including start time, completion time, success flag, error message, and fake response JSON.

SocialAccounts

Stores connected social account metadata.

Important: this table stores a SecretName reference, not an actual access token.


Local Setup

Clone the repository:

git clone git@github.com:JegMc/Listing-Auto-Poster-Sandbox.git
cd Listing-Auto-Poster-Sandbox

Restore dependencies:

dotnet restore

Move into the web project:

cd .\ListingAutoPosterSandbox.Web

Initialize user-secrets:

dotnet user-secrets init

Set your OpenAI API key:

dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY"

Set the OpenAI model:

dotnet user-secrets set "OpenAI:Model" "gpt-5-mini"

Return to the solution root:

cd ..

Install or update the EF Core CLI tool:

dotnet tool install --global dotnet-ef --version 9.0.0

If it is already installed:

dotnet tool update --global dotnet-ef --version 9.0.0
