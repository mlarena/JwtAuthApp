using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Models;

namespace JwtAuthApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<MonitoringPost> MonitoringPosts { get; set; } // Добавить эту строкю
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Настройка таблицы Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName) // Изменено с Username на UserName
                .IsUnique();

        }
    }
}