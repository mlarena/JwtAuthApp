using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;
using JwtAuthApp.Services;
using System.ComponentModel.DataAnnotations;
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

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (user == null || !_authService.VerifyPassword(model.Password, user.PasswordHash, user.Salt))
            {
                ModelState.AddModelError("", "Invalid credentials");
                return View(model);
            }

            var token = _authService.GenerateJwtToken(user);
            HttpContext.Session.SetString("JWToken", token);
            
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
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
                Role = string.IsNullOrEmpty(model.Role) ? "User" : model.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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

    public class LoginModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
        
        public string Role { get; set; }
    }
}