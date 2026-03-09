using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Models;

namespace JwtAuthApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<MonitoringPost> MonitoringPosts { get; set; } 
        public DbSet<UserActionLog> UserActionLogs { get; set; }
        
        
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Настройка таблицы Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.UserName).IsUnique();
                entity.Property(u => u.UserName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Настройка таблицы логов
            modelBuilder.Entity<UserActionLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.UserName);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.Action);
                
                entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Details).HasMaxLength(1000);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.HttpMethod).HasMaxLength(10);
                entity.Property(e => e.Url).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    
    }
}