using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Services;
using JwtAuthApp.Models;
using JwtAuthApp.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace JwtAuthApp.Controllers
{
    // Разрешаем доступ без авторизации ко всем методам этого контроллера
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public AuthController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // Убираем атрибуты маршрутизации, которые конфликтуют с HomeController
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (user == null || !_authService.VerifyPassword(model.Password, user.PasswordHash, user.Salt))
            {
                ModelState.AddModelError("", "Invalid credentials");
                return View(model);
            }

            if (user.IsBlocked)
            {
                ModelState.AddModelError("", "This account has been blocked. Contact an administrator.");
                return View(model);
            }

            var token = _authService.GenerateJwtToken(user);
            HttpContext.Session.SetString("JWToken", token);
            
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_context.Users.Any(u => u.UserName == model.UserName))
            {
                ModelState.AddModelError("", "Username already exists");
                return View(model);
            }

            var (hash, salt) = _authService.HashPassword(model.Password);
            var user = new User
            {
                UserName = model.UserName,
                PasswordHash = hash,
                Salt = salt,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Назначаем роль "User" по умолчанию
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole != null)
            {
                _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });
                await _context.SaveChangesAsync();
            }

            // Загружаем роли для генерации токена
            await _context.Entry(user)
                .Collection(u => u.UserRoles)
                .Query()
                .Include(ur => ur.Role)
                .LoadAsync();

            var token = _authService.GenerateJwtToken(user);
            HttpContext.Session.SetString("JWToken", token);
            
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // Проверяем, авторизован ли пользователь
            if (User.Identity?.IsAuthenticated == true)
            {
                HttpContext.Session.Remove("JWToken");
            }
            
            // Всегда перенаправляем на Login, независимо от статуса
            return RedirectToAction("Login");
        }
    }
}