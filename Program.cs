using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JwtAuthApp.Data;
using JwtAuthApp.Services;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using JwtAuthApp.Filters;
using JwtAuthApp.Middleware;

var builder = WebApplication.CreateBuilder(args);

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


builder.Services.AddHttpContextAccessor();
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

app.UseMiddleware<SessionTokenMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

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