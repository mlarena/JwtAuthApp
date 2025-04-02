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
    }
}