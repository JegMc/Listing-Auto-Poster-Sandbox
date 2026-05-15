using ListingAutoPosterSandbox.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ListingAutoPosterSandbox.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ScheduledPost> ScheduledPosts => Set<ScheduledPost>();
    public DbSet<PostAttempt> PostAttempts => Set<PostAttempt>();
    public DbSet<SocialAccount> SocialAccounts => Set<SocialAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduledPost>()
            .Property(post => post.Platform)
            .HasConversion<string>();

        modelBuilder.Entity<ScheduledPost>()
            .Property(post => post.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ScheduledPost>()
            .HasOne(post => post.Listing)
            .WithMany()
            .HasForeignKey(post => post.ListingId);

        modelBuilder.Entity<ScheduledPost>()
            .HasOne(post => post.SocialAccount)
            .WithMany()
            .HasForeignKey(post => post.SocialAccountId);

        modelBuilder.Entity<SocialAccount>()
            .Property(account => account.Platform)
            .HasConversion<string>();
        
        modelBuilder.Entity<PostAttempt>()
            .HasOne(attempt => attempt.ScheduledPost)
            .WithMany()
            .HasForeignKey(attempt => attempt.ScheduledPostId);

        modelBuilder.Entity<Listing>().HasData(
            new Listing
            {
                Id = 1,
                Title = "Modern Downtown Condo",
                Address = "123 Main Street, Nashville, TN",
                Price = 425000,
                Description = "A bright two-bedroom condo near restaurants, shops, and public transit.",
                ImageUrl = "https://placehold.co/600x400"
            },
            new Listing
            {
                Id = 2,
                Title = "Family Home with Large Backyard",
                Address = "456 Oak Ridge Drive, Franklin, TN",
                Price = 675000,
                Description = "A spacious four-bedroom home with an open kitchen and fenced backyard.",
                ImageUrl = "https://placehold.co/600x400"
            },
            new Listing
            {
                Id = 3,
                Title = "Quiet Townhome Near Parks",
                Address = "789 Cedar Lane, Murfreesboro, TN",
                Price = 350000,
                Description = "A low-maintenance townhome close to walking trails and local parks.",
                ImageUrl = "https://placehold.co/600x400"
            }
        );
        modelBuilder.Entity<SocialAccount>().HasData(
            new SocialAccount
            {
                Id = 1,
                Platform = PostPlatform.Facebook,
                DisplayName = "Demo Facebook Page",
                SecretName = "dev/social/facebook/demo-page",
                IsConnected = true,
                CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SocialAccount
            {
                Id = 2,
                Platform = PostPlatform.Instagram,
                DisplayName = "Demo Instagram Business Account",
                SecretName = "dev/social/instagram/demo-business",
                IsConnected = true,
                CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SocialAccount
            {
                Id = 3,
                Platform = PostPlatform.LinkedIn,
                DisplayName = "Demo LinkedIn Company Page",
                SecretName = "dev/social/linkedin/demo-company",
                IsConnected = true,
                CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}