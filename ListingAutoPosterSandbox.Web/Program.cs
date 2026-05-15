using ListingAutoPosterSandbox.Web.Data;
using ListingAutoPosterSandbox.Web.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHangfire(configuration =>
    configuration.UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();

builder.Services.AddScoped<ICaptionGenerator, OpenAiCaptionGenerator>();
builder.Services.AddScoped<ITokenStore, FakeTokenStore>();
builder.Services.AddScoped<IPlatformPoster, FakePlatformPoster>();
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


app.Run();
