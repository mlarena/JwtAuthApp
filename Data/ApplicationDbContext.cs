using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Models;

namespace JwtAuthApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserControllerAccess> UserControllerAccesses { get; set; }
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Настройка таблицы Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Настройка таблицы UserControllerAccesses
            modelBuilder.Entity<UserControllerAccess>()
                .HasIndex(uca => new { uca.UserId, uca.ControllerName })
                .IsUnique();

            // Настройка отношений
            modelBuilder.Entity<UserControllerAccess>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(uca => uca.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}