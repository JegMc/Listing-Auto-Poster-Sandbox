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
                Title = "M/Y Azure Horizon",
                Address = "Miami, FL",
                Location = "Miami, FL",
                Builder = "Sunseeker",
                BrokerageCompany = "YATCO Demo Brokerage",
                LengthFeet = 88,
                YearBuilt = 2020,
                Cabins = 4,
                Guests = 8,
                MaxSpeedKnots = 28,
                Price = 5495000,
                Description = "A sleek motor yacht with modern entertaining spaces, refined interior finishes, expansive deck areas, and strong performance for coastal cruising.",
                ImageUrl = "https://placehold.co/600x400?text=Azure+Horizon"
            },
            new Listing
            {
                Id = 2,
                Title = "M/Y Silver Current",
                Address = "Palm Beach, FL",
                Location = "Palm Beach, FL",
                Builder = "Azimut",
                BrokerageCompany = "YATCO Demo Brokerage",
                LengthFeet = 72,
                YearBuilt = 2018,
                Cabins = 4,
                Guests = 8,
                MaxSpeedKnots = 31,
                Price = 3250000,
                Description = "A well-appointed flybridge yacht designed for relaxed cruising, featuring generous outdoor lounging areas, a bright salon, and comfortable guest accommodations.",
                ImageUrl = "https://placehold.co/600x400?text=Silver+Current"
            },
            new Listing
            {
                Id = 3,
                Title = "S/Y Wind Meridian",
                Address = "Fort Lauderdale, FL",
                Location = "Fort Lauderdale, FL",
                Builder = "Beneteau",
                BrokerageCompany = "YATCO Demo Brokerage",
                LengthFeet = 58,
                YearBuilt = 2019,
                Cabins = 3,
                Guests = 6,
                MaxSpeedKnots = 12,
                Price = 875000,
                Description = "A capable sailing yacht with clean lines, efficient handling, comfortable accommodations, and a practical layout suited for extended coastal passages.",
                ImageUrl = "https://placehold.co/600x400?text=Wind+Meridian"
            }
        );
        modelBuilder.Entity<SocialAccount>().HasData(
            new SocialAccount
            {
                Id = 1,
                Platform = PostPlatform.Facebook,
                DisplayName = "Demo Facebook Page",
                SecretName = "dev/social/facebook/demo-page",
                PlatformAccountId = "1103146319551782",
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