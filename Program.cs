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
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
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
            // Проверка отзыва токена: iat должен быть позже TokenValidAfter пользователя,
            // пользователь не заблокирован (logout, смена пароля, блокировка инвалидируют токен)
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Token has no valid user identifier");
                    return;
                }

                var cache = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var cacheKey = $"token-revocation:{userId}";
                if (!cache.TryGetValue(cacheKey, out (DateTime? ValidAfter, bool IsBlocked) revocation))
                {
                    using var scope = context.HttpContext.RequestServices.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var user = await db.Users
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.TokenValidAfter, u.IsBlocked })
                        .FirstOrDefaultAsync();
                    revocation = (user?.TokenValidAfter, user?.IsBlocked ?? true);
                    cache.Set(cacheKey, revocation, TimeSpan.FromSeconds(30));
                }

                if (revocation.IsBlocked)
                {
                    context.Fail("User is blocked");
                    return;
                }

                if (revocation.ValidAfter.HasValue)
                {
                    var iatClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
                    if (iatClaim != null && long.TryParse(iatClaim, out var iatUnix))
                    {
                        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatUnix).UtcDateTime;
                        if (issuedAt < revocation.ValidAfter.Value)
                        {
                            context.Fail("Token has been revoked");
                        }
                    }
                }
            },
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
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ControllerAccessCache>();
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
app.UseMiddleware<SecurityHeadersMiddleware>();

// Инициализация БД: миграции + начальные данные (роли, суперпользователь, правила доступа)
DbSeeder.Initialize(app, builder.Configuration, app.Logger);

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