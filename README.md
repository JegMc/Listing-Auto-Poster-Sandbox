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
