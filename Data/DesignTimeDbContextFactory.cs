using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JwtAuthApp.Data
{
    // Фабрика для EF Core design-time (dotnet ef migrations add ...).
    // ApplicationDbContext имеет два конструктора (с/без IHttpContextAccessor),
    // поэтому EF не может выбрать один автоматически.
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=design;Username=postgres;Password=design")
                .Options;

            // Используем конструктор без IHttpContextAccessor (для миграций он не нужен)
            return new ApplicationDbContext(options);
        }
    }
}
