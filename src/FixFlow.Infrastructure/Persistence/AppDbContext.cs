using FixFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<TechnicianProfile> TechnicianProfiles => Set<TechnicianProfile>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestPhoto> RequestPhotos => Set<RequestPhoto>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceCategory>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasData(
                new ServiceCategory { Id = 1, Name = "تكييف", CreatedAt = new DateTime(2026, 1, 1) },
                new ServiceCategory { Id = 2, Name = "سباكة", CreatedAt = new DateTime(2026, 1, 1) },
                new ServiceCategory { Id = 3, Name = "كهرباء", CreatedAt = new DateTime(2026, 1, 1) },
                new ServiceCategory { Id = 4, Name = "نجارة", CreatedAt = new DateTime(2026, 1, 1) }
            );
        });

        modelBuilder.Entity<TechnicianProfile>(e =>
        {
            e.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            e.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
            e.HasOne(x => x.ServiceCategory)
             .WithMany(c => c.Technicians)
             .HasForeignKey(x => x.ServiceCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceRequest>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            e.Property(x => x.Address).IsRequired().HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(x => x.TrackingToken).IsUnique();

            e.HasOne(x => x.ServiceCategory)
             .WithMany(c => c.Requests)
             .HasForeignKey(x => x.ServiceCategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.TechnicianProfile)
             .WithMany(t => t.Requests)
             .HasForeignKey(x => x.TechnicianProfileId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequestPhoto>(e =>
        {
            e.Property(x => x.Url).IsRequired().HasMaxLength(500);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.ServiceRequest)
             .WithMany(r => r.Photos)
             .HasForeignKey(x => x.ServiceRequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(1000);
            e.HasOne(x => x.ServiceRequest)
             .WithOne(r => r.Review)
             .HasForeignKey<Review>(x => x.ServiceRequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.Property(x => x.Message).IsRequired().HasMaxLength(500);
        });
    }
}