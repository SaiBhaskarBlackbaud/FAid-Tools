using Microsoft.EntityFrameworkCore;
using FAid.API.Models;

namespace FAid.API.Data;

public class FAidDbContext : DbContext
{
    public FAidDbContext(DbContextOptions<FAidDbContext> options) : base(options) { }

    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Applicant> Applicants => Set<Applicant>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<FinancialData> FinancialData => Set<FinancialData>();
    public DbSet<AIReviewResult> AIReviewResults => Set<AIReviewResult>();
    public DbSet<Flag> Flags => Set<Flag>();
    public DbSet<ReviewQueue> ReviewQueues => Set<ReviewQueue>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasIndex(a => a.ApplicationNumber).IsUnique();
            entity.HasMany(a => a.Applicants).WithOne(ap => ap.Application)
                  .HasForeignKey(ap => ap.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(a => a.Documents).WithOne(d => d.Application)
                  .HasForeignKey(d => d.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(a => a.FinancialData).WithOne(fd => fd.Application)
                  .HasForeignKey(fd => fd.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.AIReviewResult).WithOne(r => r.Application)
                  .HasForeignKey<AIReviewResult>(r => r.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.ReviewQueue).WithOne(q => q.Application)
                  .HasForeignKey<ReviewQueue>(q => q.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<AIReviewResult>(entity =>
        {
            entity.HasMany(r => r.Flags).WithOne(f => f.AIReviewResult)
                  .HasForeignKey(f => f.ReviewId).OnDelete(DeleteBehavior.Cascade);
        });

        // Seed admin user with bcrypt hash for "Admin@123"
        modelBuilder.Entity<User>().HasData(new User
        {
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Username = "admin",
            EmailAddress = "admin@faid.example.com",
            PasswordHash = "$2a$11$HJpSqJ0dR3Wm.pxdJjqJD.8KfM1MZ0r8W7r1Vq.vxLSqWaKj.g3i",
            FullName = "System Administrator",
            Role = "Admin",
            IsActive = true,
            CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
