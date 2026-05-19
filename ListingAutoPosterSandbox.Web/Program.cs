using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// here we add services to the dependency injection container. This includes framework services like controllers and session management, 
//as well as our own application services for handling Facebook OAuth, token storage, caption generation, and scheduled post publishing.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".ListingAutoPosterSandbox.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//AddDbContext 
// here we use the same SQL Server database for both the application data and Hangfire's job storage for simplicity.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

//Addhangfire 
builder.Services.AddHangfire(configuration =>
    configuration.UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();

// here we configure options for our Facebook integration by binding the "Facebook" section of our configuration (which could come from appsettings.json, environment variables, 
// or user secrets) to a strongly-typed FacebookOptions class. This allows us to easily access these settings throughout our application via dependency injection.
builder.Services.Configure<ListingAutoPosterSandbox.Web.Services.FacebookOptions>(
    builder.Configuration.GetSection("Facebook"));

//AddHtpClient
builder.Services.AddHttpClient<ListingAutoPosterSandbox.Web.Services.FacebookPagePoster>();
builder.Services.AddHttpClient<ListingAutoPosterSandbox.Web.Services.FacebookOAuthService>();

builder.Services.AddScoped<ListingAutoPosterSandbox.Web.Services.IPlatformPoster>(
    serviceProvider =>
        serviceProvider.GetRequiredService<ListingAutoPosterSandbox.Web.Services.FacebookPagePoster>());

//AddSingleton 
builder.Services.AddSingleton<ListingAutoPosterSandbox.Web.Services.LocalFacebookTokenStore>();

builder.Services.AddSingleton<ListingAutoPosterSandbox.Web.Services.ITokenStore>(
    serviceProvider =>
        serviceProvider.GetRequiredService<ListingAutoPosterSandbox.Web.Services.LocalFacebookTokenStore>());

//Addscoped
// here we register our application services with the dependency injection container. This allows us to inject these services into controllers or other services as needed.
builder.Services.AddScoped<ICaptionGenerator, OpenAiCaptionGenerator>();
builder.Services.AddScoped<IScheduledPostPublisher, ScheduledPostPublisher>();
builder.Services.AddScoped<IDuePostScanner, DuePostScanner>();

//builder.Services.AddScoped<ITokenStore, FakeTokenStore>();
//builder.Services.AddScoped<IPlatformPoster, FakePlatformPoster>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();

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

app.Run();
