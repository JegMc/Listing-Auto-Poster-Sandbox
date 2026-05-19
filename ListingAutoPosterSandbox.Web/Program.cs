using Hangfire;
using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC is the web framework layer. It lets the app use Controllers, Views, and Razor pages.
builder.Services.AddControllersWithViews();

// Authorization is registered now so controller/action authorization can be added later if needed.
builder.Services.AddAuthorization();

// Session stores temporary browser-session data.
// This app uses it during flows like Facebook OAuth, where short-lived state may need to survive redirects.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".ListingAutoPosterSandbox.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Main application database.
// This stores app data such as Listings, ScheduledPosts, PostAttempts, and SocialAccounts.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Hangfire uses SQL Server to store background job data.
// For this sandbox, Hangfire uses the same SQL Server connection as the main app.
builder.Services.AddHangfire(configuration =>
    configuration.UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();

// Facebook settings are loaded from configuration.
// In local development, sensitive values should come from user secrets or environment variables,
// not from committed appsettings files.
builder.Services.Configure<FacebookOptions>(
    builder.Configuration.GetSection("Facebook"));

// Typed HttpClients give these Facebook services a reusable, DI-managed HttpClient.
builder.Services.AddHttpClient<FacebookPagePoster>();
builder.Services.AddHttpClient<FacebookOAuthService>();

// IPlatformPoster is the app-level posting abstraction.
// The active implementation is FacebookPagePoster, so scheduled publishing posts to Facebook.
builder.Services.AddScoped<IPlatformPoster>(serviceProvider =>
    serviceProvider.GetRequiredService<FacebookPagePoster>());

// LocalFacebookTokenStore stores Facebook tokens locally for this sandbox.
// It is registered as a singleton so the same file-backed token store is reused across the app.
builder.Services.AddSingleton<LocalFacebookTokenStore>();
builder.Services.AddSingleton<ITokenStore>(serviceProvider =>
    serviceProvider.GetRequiredService<LocalFacebookTokenStore>());

// Application services.
builder.Services.AddScoped<ICaptionGenerator, OpenAiCaptionGenerator>();
builder.Services.AddScoped<IScheduledPostPublisher, ScheduledPostPublisher>();
builder.Services.AddScoped<IDuePostScanner, DuePostScanner>();

var app = builder.Build();

// Production-only error handling.
// In Development, ASP.NET shows detailed error pages to make debugging easier.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

// Every minute, Hangfire asks the app to find scheduled posts that are due.
// DuePostScanner then enqueues publish work through the normal ScheduledPost pipeline.
RecurringJob.AddOrUpdate<IDuePostScanner>(
    "enqueue-due-scheduled-posts",
    scanner => scanner.EnqueueDuePostsAsync(),
    Cron.Minutely);

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();