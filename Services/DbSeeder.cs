using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Services
{
    // Инициализация БД: миграции + начальные данные (роли, суперпользователь, правила доступа)
    public static class DbSeeder
    {
        public static void Initialize(IApplicationBuilder app, IConfiguration configuration, ILogger logger)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Применяем миграции
            dbContext.Database.Migrate();

            SeedRoles(dbContext);
            SeedSuperUser(dbContext, scope, configuration, logger);
            AssignMissingRoles(dbContext);
            SeedControllerAccess(dbContext);
            ControllerDiscoveryService.DiscoverAndRegister(app);
        }

        private static void SeedRoles(ApplicationDbContext db)
        {
            if (!db.Roles.Any())
            {
                db.Roles.AddRange(
                    new Role { Name = "Admin", Description = "Системный администратор" },
                    new Role { Name = "User", Description = "Обычный пользователь" }
                );
                db.SaveChanges();
            }
        }

        private static void SeedSuperUser(ApplicationDbContext db, IServiceScope scope, IConfiguration configuration, ILogger logger)
        {
            // Пароль берётся из Security:SuperUserPassword (env: Security__SuperUserPassword);
            // если не задан — генерируется случайный и логируется один раз.
            if (db.Users.Any(u => u.UserName == "su"))
                return;

            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var superUserPassword = configuration["Security:SuperUserPassword"];
            if (string.IsNullOrWhiteSpace(superUserPassword))
            {
                superUserPassword = System.Security.Cryptography.RandomNumberGenerator.GetHexString(24);
                logger.LogWarning(
                    "Security:SuperUserPassword не задан. Для пользователя 'su' сгенерирован случайный пароль: {Password}. " +
                    "Смените его и сохраните в безопасном месте.",
                    superUserPassword);
            }

            var (hash, salt) = authService.HashPassword(superUserPassword);

            var superUser = new User
            {
                UserName = "su",
                PasswordHash = hash,
                Salt = salt,
                Role = "Admin"
            };

            db.Users.Add(superUser);
            db.SaveChanges();

            // Назначаем роль Admin
            var adminRole = db.Roles.FirstOrDefault(r => r.Name == "Admin");
            if (adminRole != null)
            {
                db.UserRoles.Add(new UserRole { UserId = superUser.Id, RoleId = adminRole.Id });
                db.SaveChanges();
            }
        }

        private static void AssignMissingRoles(ApplicationDbContext db)
        {
            // Назначаем роли существующим пользователям, у которых нет UserRole записей
            var usersWithoutRoles = db.Users
                .Where(u => !db.UserRoles.Any(ur => ur.UserId == u.Id))
                .ToList();
            if (!usersWithoutRoles.Any())
                return;

            var userRole = db.Roles.FirstOrDefault(r => r.Name == "User");
            var adminRole = db.Roles.FirstOrDefault(r => r.Name == "Admin");
            if (userRole == null)
                return;

            foreach (var u in usersWithoutRoles)
            {
                var roleToAssign = u.Role == "Admin" ? adminRole : userRole;
                if (roleToAssign != null)
                {
                    db.UserRoles.Add(new UserRole { UserId = u.Id, RoleId = roleToAssign.Id });
                }
            }
            db.SaveChanges();
        }

        private static void SeedControllerAccess(ApplicationDbContext db)
        {
            // Seed ControllerAccess если таблица пуста
            if (db.ControllerAccesses.Any())
                return;

            var adminRole = db.Roles.FirstOrDefault(r => r.Name == "Admin");
            var userRole = db.Roles.FirstOrDefault(r => r.Name == "User");

            // (controller, display, description, allowAllAuthenticated)
            var entries = new (string Controller, string Display, string? Description, bool AllowAll)[]
            {
                ("Secure", "Защищённая", "Защищённая страница", false),
                ("Test", "Тест", "Тестовая страница", false),
                ("MonitoringPost", "Посты мониторинга", "Посты мониторинга (CRUD)", false),
                ("Sensor", "Датчики", "Датчики (CRUD)", false),
                ("SensorType", "Типы датчиков", "Справочник типов датчиков", false),
                ("DOVData", "Данные DOV", "Данные видимости (DOV)", false),
                ("DSPDData", "Данные DSPD", "Данные состояния дороги (DSPD)", false),
                ("DustData", "Данные пыли", "Данные пыли (PM10/PM2.5/PM1)", false),
                ("IWSData", "Метеоданные IWS", "Метеоданные (IWS)", false),
                ("MUEKSData", "Данные MUEKS", "Данные питания/системы (MUEKS)", false),
                ("PollingSession", "Сессии опроса", "Сессии опроса датчиков", false),
                ("SensorResults", "Результаты опроса", "Результаты опроса датчиков", false),
                ("Admin", "Пользователи", "Управление пользователями", true),
                ("Role", "Роли", "Управление ролями", true),
                ("Access", "Управление доступом", "Правила доступа к контроллерам", true),
                ("Audit", "Журнал аудита", "Журнал действий системы", true)
            };

            var accessEntries = entries.Select(e => new ControllerAccess
            {
                ControllerName = e.Controller,
                DisplayName = e.Display,
                Description = e.Description,
                AllowAllAuthenticated = e.AllowAll
            }).ToList();

            db.ControllerAccesses.AddRange(accessEntries);
            db.SaveChanges();

            // Назначаем роли (User + Admin) всем записям, кроме AllowAll
            foreach (var entry in accessEntries)
            {
                if (entry.AllowAllAuthenticated)
                    continue;

                var saved = db.ControllerAccesses.First(c => c.ControllerName == entry.ControllerName);
                if (adminRole != null)
                    db.ControllerAccessRoles.Add(new ControllerAccessRole { ControllerAccessId = saved.Id, RoleId = adminRole.Id });
                if (userRole != null)
                    db.ControllerAccessRoles.Add(new ControllerAccessRole { ControllerAccessId = saved.Id, RoleId = userRole.Id });
            }
            db.SaveChanges();
        }
    }
}
