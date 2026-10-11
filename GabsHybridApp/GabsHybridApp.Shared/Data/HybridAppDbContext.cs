using GabsHybridApp.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace GabsHybridApp.Shared.Data;

public class HybridAppDbContext : DbContext
{
    public HybridAppDbContext(DbContextOptions<HybridAppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        try
        {
            var schema = this.GetService<IConfiguration>()?.GetConnectionString("Schema");
            if (!string.IsNullOrWhiteSpace(schema) && !Database.IsSqlite())
            {
                modelBuilder.HasDefaultSchema(schema);
            }
        }
        catch
        {
            // Ignore during design-time factory execution without IConfiguration
        }

        // Product config
        modelBuilder.Entity<Product>(e =>
        {
            // Seed data (uses your static SeedData() list)
            e.HasData(Product.SeedData());
        });

        // Order config
        modelBuilder.Entity<Order>(e =>
        {
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasMany(o => o.Items)
             .WithOne(i => i.Order)
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // RegisteredDevice config
        modelBuilder.Entity<RegisteredDevice>(e =>
        {
            e.HasIndex(d => d.DeviceId).IsUnique();
        });

        // Assessment config
        modelBuilder.Entity<Assessment>(e =>
        {
            e.HasMany(a => a.Keys)
             .WithOne(k => k.Assessment)
             .HasForeignKey(k => k.AssessmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(a => a.Scans)
             .WithOne(s => s.Assessment)
             .HasForeignKey(s => s.AssessmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Student config
        modelBuilder.Entity<Student>(e =>
        {
            e.HasIndex(s => s.StudentIdNumber).IsUnique();
            e.HasMany(s => s.Scans)
             .WithOne(sc => sc.Student)
             .HasForeignKey(sc => sc.StudentId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // AnswerSheetScan config
        modelBuilder.Entity<AnswerSheetScan>(e =>
        {
            e.HasIndex(s => new { s.AssessmentId, s.DetectedStudentIdNumber });
        });
    }

    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<RegisteredDevice> RegisteredDevices { get; set; }
    public DbSet<BackupRecord> BackupRecords { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<AssessmentKey> AssessmentKeys { get; set; }
    public DbSet<AnswerSheetScan> AnswerSheetScans { get; set; }
    public DbSet<Student> Students { get; set; }
}