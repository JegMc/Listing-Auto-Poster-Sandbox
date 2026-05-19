using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// here we add services to the dependency injection container. This is where we register our application's services, database context, and any third-party services we need (like Hangfire for background jobs).
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// here we use the same SQL Server database for both the application data and Hangfire's job storage for simplicity.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHangfire(configuration =>
    configuration.UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();

// here we configure options for our Facebook integration by binding the "Facebook" section of our configuration (which could come from appsettings.json, environment variables, 
// or user secrets) to a strongly-typed FacebookOptions class. This allows us to easily access these settings throughout our application via dependency injection.
builder.Services.Configure<ListingAutoPosterSandbox.Web.Services.FacebookOptions>(
    builder.Configuration.GetSection("Facebook"));

builder.Services.AddHttpClient<ListingAutoPosterSandbox.Web.Services.FacebookPagePoster>();

builder.Services.AddScoped<ListingAutoPosterSandbox.Web.Services.IPlatformPoster>(
    serviceProvider =>
        serviceProvider.GetRequiredService<ListingAutoPosterSandbox.Web.Services.FacebookPagePoster>());

builder.Services.AddScoped<ListingAutoPosterSandbox.Web.Services.ITokenStore,
    ListingAutoPosterSandbox.Web.Services.UserSecretsFacebookTokenStore>();

// here we register our application services with the dependency injection container. This allows us to inject these services into controllers or other services as needed.
builder.Services.AddScoped<ICaptionGenerator, OpenAiCaptionGenerator>();
//builder.Services.AddScoped<ITokenStore, FakeTokenStore>();
//builder.Services.AddScoped<IPlatformPoster, FakePlatformPoster>();
builder.Services.AddScoped<IScheduledPostPublisher, ScheduledPostPublisher>();
builder.Services.AddScoped<IDuePostScanner, DuePostScanner>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<IDuePostScanner>(
    "enqueue-due-scheduled-posts",
    scanner => scanner.EnqueueDuePostsAsync(),
    Cron.Minutely);

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ListingAutoPosterSandbox.Web.Data.AppDbContext>();

    var facebookAccount = db.SocialAccounts
        .FirstOrDefault(account => account.Platform == ListingAutoPosterSandbox.Web.Models.PostPlatform.Facebook);

    if (facebookAccount is not null)
    {
        facebookAccount.PlatformAccountId = "1103146319551782";
        facebookAccount.IsConnected = true;
        facebookAccount.UpdatedUtc = DateTime.UtcNow;
        db.SaveChanges();
    }
}

app.Run();
