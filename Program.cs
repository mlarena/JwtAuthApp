using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JwtAuthApp.Data;
using JwtAuthApp.Services;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using JwtAuthApp.Filters;
using JwtAuthApp.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Валидация обязательной конфигурации (секреты передаются через переменные окружения):
//   ConnectionStrings__DefaultConnection, Jwt__Key, Security__SuperUserPassword
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Строка подключения не задана. Установите переменную окружения ConnectionStrings__DefaultConnection.");
}

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT-ключ не задан или короче 32 символов. Установите переменную окружения Jwt__Key.");
}

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
}, ServiceLifetime.Scoped);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        
        // Добавляем обработку события, когда токен не валиден
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();

                if (context.Request.Headers.ContainsKey("Accept") &&
                    context.Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                }

                context.Response.Redirect("/Auth/Login");
                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                if (context.Request.Headers.ContainsKey("Accept") &&
                    context.Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsJsonAsync(new { error = "Forbidden" });
                }

                context.Response.Redirect("/Auth/Login");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
// Настройка MVC с глобальным фильтром логирования
builder.Services.AddControllersWithViews(options =>
{
    // Добавляем фильтр логирования глобально
    options.Filters.Add<UserActionLogFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Не забудьте зарегистрировать фильтр как Scoped
builder.Services.AddScoped<UserActionLogFilter>();

// Rate limiting для защиты от брутфорса на Login/Register
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Добавляем Antiforgery с правильной настройкой для HTTP/HTTPS
builder.Services.AddAntiforgery(options => 
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "AntiForgeryCookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});


builder.Services.AddHttpContextAccessor();
// Пробрасываем заголовки reverse-proxy (nginx), чтобы RemoteIpAddress/схема были реальными
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                             | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // За типичным nginx-деплоем доверяем всем прокси; при необходимости ограничьте KnownProxies
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Настройка миграций базы данных
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    // Создаем роли, если их нет
    if (!dbContext.Roles.Any())
    {
        dbContext.Roles.AddRange(
            new JwtAuthApp.Models.Role { Name = "Admin", Description = "Системный администратор" },
            new JwtAuthApp.Models.Role { Name = "User", Description = "Обычный пользователь" }
        );
        dbContext.SaveChanges();
    }

    // Создаем суперпользователя, если его нет.
    // Пароль берётся из Security:SuperUserPassword (env: Security__SuperUserPassword);
    // если не задан — генерируется случайный и логируется один раз.
    if (!dbContext.Users.Any(u => u.UserName == "su"))
    {
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var superUserPassword = builder.Configuration["Security:SuperUserPassword"];
        if (string.IsNullOrWhiteSpace(superUserPassword))
        {
            superUserPassword = System.Security.Cryptography.RandomNumberGenerator.GetHexString(24);
            app.Logger.LogWarning(
                "Security:SuperUserPassword не задан. Для пользователя 'su' сгенерирован случайный пароль: {Password}. " +
                "Смените его и сохраните в безопасном месте.",
                superUserPassword);
        }
        var (hash, salt) = authService.HashPassword(superUserPassword);

        var superUser = new JwtAuthApp.Models.User
        {
            UserName = "su",
            PasswordHash = hash,
            Salt = salt,
            Role = "Admin"
        };

        dbContext.Users.Add(superUser);
        dbContext.SaveChanges();

        // Назначаем роль Admin
        var adminRole = dbContext.Roles.FirstOrDefault(r => r.Name == "Admin");
        if (adminRole != null)
        {
            dbContext.UserRoles.Add(new JwtAuthApp.Models.UserRole
            {
                UserId = superUser.Id,
                RoleId = adminRole.Id
            });
            dbContext.SaveChanges();
        }
    }

    // Назначаем роли существующим пользователям, у которых нет UserRole записей
    var usersWithoutRoles = dbContext.Users
        .Where(u => !dbContext.UserRoles.Any(ur => ur.UserId == u.Id))
        .ToList();
    if (usersWithoutRoles.Any())
    {
        var userRole = dbContext.Roles.FirstOrDefault(r => r.Name == "User");
        var adminRole = dbContext.Roles.FirstOrDefault(r => r.Name == "Admin");
        if (userRole != null)
        {
            foreach (var u in usersWithoutRoles)
            {
                var roleToAssign = u.Role == "Admin" ? adminRole : userRole;
                if (roleToAssign != null)
                {
                    dbContext.UserRoles.Add(new JwtAuthApp.Models.UserRole
                    {
                        UserId = u.Id,
                        RoleId = roleToAssign.Id
                    });
                }
            }
            dbContext.SaveChanges();
        }
    }

    // Seed ControllerAccess если таблица пуста
    if (!dbContext.ControllerAccesses.Any())
    {
        var adminRoleForSeed = dbContext.Roles.FirstOrDefault(r => r.Name == "Admin");
        var userRoleForSeed = dbContext.Roles.FirstOrDefault(r => r.Name == "User");

        var accessEntries = new List<JwtAuthApp.Models.ControllerAccess>();

        void AddAccess(string controller, string display, string? desc, bool allowAll, string[]? roleNames)
        {
            var entry = new JwtAuthApp.Models.ControllerAccess
            {
                ControllerName = controller,
                DisplayName = display,
                Description = desc,
                AllowAllAuthenticated = allowAll
            };
            accessEntries.Add(entry);
        }

        AddAccess("Secure", "Защищённая", "Защищённая страница", false, new[] { "User", "Admin" });
        AddAccess("Test", "Тест", "Тестовая страница", false, new[] { "User", "Admin" });
        AddAccess("MonitoringPost", "Посты мониторинга", "Посты мониторинга (CRUD)", false, new[] { "User", "Admin" });
        AddAccess("Sensor", "Датчики", "Датчики (CRUD)", false, new[] { "User", "Admin" });
        AddAccess("SensorType", "Типы датчиков", "Справочник типов датчиков", false, new[] { "User", "Admin" });
        AddAccess("DOVData", "Данные DOV", "Данные видимости (DOV)", false, new[] { "User", "Admin" });
        AddAccess("DSPDData", "Данные DSPD", "Данные состояния дороги (DSPD)", false, new[] { "User", "Admin" });
        AddAccess("DustData", "Данные пыли", "Данные пыли (PM10/PM2.5/PM1)", false, new[] { "User", "Admin" });
        AddAccess("IWSData", "Метеоданные IWS", "Метеоданные (IWS)", false, new[] { "User", "Admin" });
        AddAccess("MUEKSData", "Данные MUEKS", "Данные питания/системы (MUEKS)", false, new[] { "User", "Admin" });
        AddAccess("PollingSession", "Сессии опроса", "Сессии опроса датчиков", false, new[] { "User", "Admin" });
        AddAccess("SensorResults", "Результаты опроса", "Результаты опроса датчиков", false, new[] { "User", "Admin" });
        AddAccess("Admin", "Пользователи", "Управление пользователями", true, null);
        AddAccess("Role", "Роли", "Управление ролями", true, null);
        AddAccess("Access", "Управление доступом", "Правила доступа к контроллерам", true, null);
        AddAccess("Audit", "Журнал аудита", "Журнал действий системы", true, null);

        dbContext.ControllerAccesses.AddRange(accessEntries);
        dbContext.SaveChanges();

        // Назначаем роли к записям
        foreach (var entry in accessEntries)
        {
            var saved = dbContext.ControllerAccesses.First(c => c.ControllerName == entry.ControllerName);
            if (entry.AllowAllAuthenticated)
            {
                // AllowAll — не привязываем роли
                continue;
            }
            // Привязываем все роли (User + Admin)
            if (adminRoleForSeed != null)
                dbContext.ControllerAccessRoles.Add(new JwtAuthApp.Models.ControllerAccessRole { ControllerAccessId = saved.Id, RoleId = adminRoleForSeed.Id });
            if (userRoleForSeed != null)
                dbContext.ControllerAccessRoles.Add(new JwtAuthApp.Models.ControllerAccessRole { ControllerAccessId = saved.Id, RoleId = userRoleForSeed.Id });
        }
        dbContext.SaveChanges();
    }

    // Автообнаружение новых контроллеров
    ControllerDiscoveryService.DiscoverAndRegister(app);
}

// Конфигурация пайплайна
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// В разработке используем HTTP, в продакшене - HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();

app.UseMiddleware<SessionTokenMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ControllerAccessMiddleware>();
app.UseMiddleware<AuthRedirectMiddleware>();

// Настройка маршрутов
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Обработка корневого маршрута
app.MapGet("/", context =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/Home/Index");
    }
    else
    {
        context.Response.Redirect("/Auth/Login");
    }
    return Task.CompletedTask;
});

app.Run();