# Retired Facebook Test Endpoint

This folder preserves the old Facebook test endpoint for learning/reference.

## Original purpose

The old `FacebookTestController` was created during early Facebook posting tests. It let the app manually create a `ScheduledPost`, send it through `ScheduledPostPublisher`, create a `PostAttempt`, and publish to the Facebook test Page.

## Why it was retired

The app now has a better user-facing flow:

1. User starts from a Listing.
2. The app generates an AI Facebook caption.
3. User reviews/edits the post.
4. User publishes the reviewed post.
5. The app creates a `ScheduledPost`.
6. `ScheduledPostPublisher` publishes through `FacebookPagePoster`.

Because of that newer flow, the old `/FacebookTest` endpoint is no longer needed in the active MVC app.

## Archived files

- `FacebookTestController.cs.txt`
- `FacebookTestViewModel.cs.txt`
- `Index.cshtml.txt`

These are intentionally saved as `.txt` files so they are not compiled or treated as active Razor/C# files.