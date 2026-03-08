using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JwtAuthApp.Data;
using JwtAuthApp.Services;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
                // Пропускаем дефолтную логику
                context.HandleResponse();
                
                // Перенаправляем на страницу логина
                context.Response.Redirect("/Auth/Login");
                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                // Перенаправляем на страницу логина при недостаточных правах
                context.Response.Redirect("/Auth/Login");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Добавляем CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Добавляем Antiforgery с правильной настройкой для HTTP/HTTPS
builder.Services.AddAntiforgery(options => 
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "AntiForgeryCookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Добавляем Swagger
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Настройка миграций базы данных
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    
    // Создаем суперпользователя, если его нет
    if (!dbContext.Users.Any(u => u.UserName == "su"))
    {
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var (hash, salt) = authService.HashPassword("su");
        
        var superUser = new JwtAuthApp.Models.User
        {
            UserName = "su",
            PasswordHash = hash,
            Salt = salt,
            Role = "Admin"
        };
        
        dbContext.Users.Add(superUser);
        dbContext.SaveChanges();
    }
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
app.UseCors("AllowAll");
app.UseSession();

// Middleware для добавления токена в заголовок
app.Use(async (context, next) =>
{
    var token = context.Session.GetString("JWToken");
    if (!string.IsNullOrEmpty(token))
    {
        context.Request.Headers["Authorization"] = "Bearer " + token;
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
// Middleware для обработки 404 ошибок
app.Use(async (context, next) =>
{
    await next();
    
    if (context.Response.StatusCode == 404 && !context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/Auth/Login");
    }
    else if (context.Response.StatusCode == 404 && context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/Home/Index");
    }
});
// Middleware для обработки маршрутов
app.Use(async (context, next) =>
{
    // Пропускаем запросы к статическим файлам
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib"))
    {
        await next();
        return;
    }

    // Обработка пустого маршрута Auth
    if (context.Request.Path.Equals("/Auth") || 
        context.Request.Path.Equals("/Auth/"))
    {
        context.Response.Redirect("/Auth/Login");
        return;
    }

    // Проверяем, является ли запрос POST запросом на Logout
    bool isLogoutPost = context.Request.Method == "POST" && 
                        context.Request.Path.Equals("/Auth/Logout");

    // Если пользователь не авторизован и пытается получить доступ не к Auth контроллеру
    if (!context.User.Identity?.IsAuthenticated == true && 
        !context.Request.Path.StartsWithSegments("/Auth") &&
        !isLogoutPost)
    {
        context.Response.Redirect("/Auth/Login");
        return;
    }

    // Если пользователь авторизован и пытается получить доступ к Auth контроллеру (кроме Logout)
    if (context.User.Identity?.IsAuthenticated == true && 
        context.Request.Path.StartsWithSegments("/Auth") &&
        !context.Request.Path.Equals("/Auth/Logout"))
    {
        context.Response.Redirect("/Home/Index");
        return;
    }

    await next();
});

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