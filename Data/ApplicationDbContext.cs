using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking; // Этого импорта не хватало!
using JwtAuthApp.Models;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace JwtAuthApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public DbSet<User> Users { get; set; }
        public DbSet<MonitoringPost> MonitoringPosts { get; set; } 
        public DbSet<UserActionLog> UserActionLogs { get; set; }
        public DbSet<EntityChangeLog> EntityChangeLogs { get; set; }
        
        // Конструктор с IHttpContextAccessor
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Конструктор без IHttpContextAccessor (для миграций)
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

            // Настройка таблицы логов действий
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

            // Настройка таблицы логов изменений сущностей
            modelBuilder.Entity<EntityChangeLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasIndex(e => e.EntityId);
                entity.HasIndex(e => e.EntityType);
                entity.HasIndex(e => e.ChangeType);
                entity.HasIndex(e => e.Timestamp);
                
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OriginalValues).HasColumnType("jsonb");
                entity.Property(e => e.NewValues).HasColumnType("jsonb");
                entity.Property(e => e.ChangedProperties).HasColumnType("jsonb");
                entity.Property(e => e.UserName).HasMaxLength(100);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Получаем информацию о пользователе
            string? userName = null;
            int? userId = null;
            
            if (_httpContextAccessor?.HttpContext?.User.Identity?.IsAuthenticated == true)
            {
                userName = _httpContextAccessor.HttpContext.User.Identity.Name;
                var userIdClaim = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim, out var parsedUserId))
                    userId = parsedUserId;
            }

            // Отслеживаем изменения до сохранения
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || 
                           e.State == EntityState.Modified || 
                           e.State == EntityState.Deleted)
                .ToList();

            // Список для логов изменений
            var changeLogs = new List<EntityChangeLog>();

            // Логируем изменения
            foreach (var entry in entries)
            {
                var log = CreateEntityChangeLog(entry, userName, userId);
                if (log != null)
                {
                    changeLogs.Add(log);
                }
            }

            // Добавляем логи в контекст
            foreach (var log in changeLogs)
            {
                await EntityChangeLogs.AddAsync(log, cancellationToken);
            }

            // Сохраняем все изменения (включая логи)
            return await base.SaveChangesAsync(cancellationToken);
        }

        private EntityChangeLog? CreateEntityChangeLog(EntityEntry entry, string? userName, int? userId)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);
            
            // Пропускаем логирование самих логов
            if (entityType.Contains("Log"))
                return null;

            var changeType = entry.State.ToString();
            var log = new EntityChangeLog
            {
                EntityType = entityType,
                EntityId = entityId,
                ChangeType = changeType,
                UserName = userName ?? "System",
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        log.NewValues = JsonSerializer.Serialize(entry.CurrentValues.ToObject());
                        break;

                    case EntityState.Deleted:
                        log.OriginalValues = JsonSerializer.Serialize(entry.OriginalValues.ToObject());
                        break;

                    case EntityState.Modified:
                        var original = entry.OriginalValues.ToObject();
                        var current = entry.CurrentValues.ToObject();
                        
                        log.OriginalValues = JsonSerializer.Serialize(original);
                        log.NewValues = JsonSerializer.Serialize(current);
                        
                        // Определяем какие свойства изменились
                        var changedProps = entry.Properties
                            .Where(p => p.IsModified && !p.Metadata.Name.Contains("Password", StringComparison.OrdinalIgnoreCase))
                            .Select(p => new
                            {
                                Property = p.Metadata.Name,
                                OldValue = p.OriginalValue?.ToString(),
                                NewValue = p.CurrentValue?.ToString()
                            })
                            .ToList();
                        
                        if (changedProps.Any())
                        {
                            log.ChangedProperties = JsonSerializer.Serialize(changedProps);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating change log: {ex.Message}");
                return null;
            }

            return log;
        }

        private int? GetEntityId(EntityEntry entry)
        {
            try
            {
                var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
                if (idProperty != null)
                {
                    if (entry.State == EntityState.Added)
                        return null;
                    
                    return idProperty.CurrentValue as int?;
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
            return null;
        }
    }
}