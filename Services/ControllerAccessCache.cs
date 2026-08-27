using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Services
{
    // Кэш правил доступа к контроллерам, чтобы не ходить в БД на каждый запрос.
    // Инвалидация — полная (сброс всех записей), вызывается из AccessController.
    public class ControllerAccessCache
    {
        private const string VersionKey = "__version";
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

        public ControllerAccessCache(IMemoryCache cache, IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        // Текущая версия кэша; при инвалидации версия меняется, старые записи игнорируются
        private long Version => _cache.Get<long>(VersionKey);

        private string KeyFor(string controllerName) => $"ctrl-access:{Version}:{controllerName}";

        public async Task<ControllerAccess?> GetAsync(string controllerName, CancellationToken ct = default)
        {
            var key = KeyFor(controllerName);
            // Храним object: null-значение тоже кэшируется (отрицательный результат)
            if (_cache.TryGetValue(key, out object? cached))
                return (ControllerAccess?)cached;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var access = await db.ControllerAccesses
                .AsNoTracking()
                .Include(c => c.ControllerAccessRoles)
                    .ThenInclude(cr => cr.Role)
                .FirstOrDefaultAsync(c => c.ControllerName == controllerName, ct);

            // Найденное правило — на 5 минут, отсутствие записи — на 30 секунд
            _cache.Set(key, (object?)access, access != null ? DefaultTtl : TimeSpan.FromSeconds(30));

            return access;
        }

        // Сбрасывает весь кэш (после изменения правил доступа)
        public void Invalidate()
        {
            _cache.Set(VersionKey, Version + 1);
        }
    }
}
