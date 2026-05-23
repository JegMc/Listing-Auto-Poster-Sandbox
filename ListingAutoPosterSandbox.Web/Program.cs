using Hangfire;
using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Services;
using Microsoft.EntityFrameworkCore;
using ListingAutoPosterSandbox.Web.Services.Facebook;

var builder = WebApplication.CreateBuilder(args);

// MVC / Razor view support.
builder.Services.AddControllersWithViews();

// Authorization is registered for future controller/action authorization.
// The app does not have user login/authentication yet.
builder.Services.AddAuthorization();

// Session is used by the Facebook OAuth flow to store short-lived redirect state.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".ListingAutoPosterSandbox.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

// Main application database.
// Stores Listings, ScheduledPosts, PostAttempts, and SocialAccounts.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Hangfire background job storage.
// For this sandbox, Hangfire uses the same LocalDB connection as the app.
builder.Services.AddHangfire(configuration =>
    configuration.UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();

// Facebook/Instagram configuration.
// Sensitive values should come from user-secrets or environment variables,
// not committed appsettings files.
builder.Services.Configure<FacebookOptions>(
    builder.Configuration.GetSection("Facebook"));

// Instagram posting is not implemented yet, but the scaffold poster and diagnostics are registered
builder.Services.Configure<InstagramDiagnosticOptions>(
    builder.Configuration.GetSection("InstagramDiagnostic"));

builder.Services.AddHttpClient<IInstagramConnectionDiagnosticService, InstagramConnectionDiagnosticService>();

// Typed HTTP client for Instagram poster. This is registered even though the poster is not active yet,
builder.Services.AddHttpClient<InstagramPlatformPoster>();

builder.Services.AddScoped<IPlatformPoster>(
    serviceProvider => serviceProvider.GetRequiredService<InstagramPlatformPoster>());

// Typed HTTP clients for Meta Graph API services.
builder.Services.AddHttpClient<FacebookPagePoster>();
builder.Services.AddHttpClient<FacebookOAuthService>();

// Active social publishing path.
// ScheduledPostPublisher depends on IPlatformPoster, and this points to the real Facebook poster.
builder.Services.AddScoped<IPlatformPoster>(serviceProvider =>
    serviceProvider.GetRequiredService<FacebookPagePoster>());

// Active local token path.
// LocalFacebookTokenStore writes to App_Data/facebook-tokens.local.json for sandbox testing.
builder.Services.AddSingleton<LocalFacebookTokenStore>();
builder.Services.AddSingleton<ITokenStore>(serviceProvider =>
    serviceProvider.GetRequiredService<LocalFacebookTokenStore>());

// Application services.
builder.Services.AddScoped<ICaptionGenerator, OpenAiCaptionGenerator>();
builder.Services.AddScoped<IScheduledPostPublisher, ScheduledPostPublisher>();
builder.Services.AddScoped<IDuePostScanner, DuePostScanner>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseSession();
app.UseAuthorization();

// Hangfire Dashboard is useful locally, but should not be exposed in production
// until authentication/authorization is added.
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Every minute, Hangfire asks the app to enqueue posts that are due.
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